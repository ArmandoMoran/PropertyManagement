"""
Verify Rose Residential income statements (PDFs) against PropertyManagement database.

The PDF income statements from the property manager only include:
  - Income: Rent Income (and sometimes other PM-managed income)
  - Expenses: Management Fee, Repairs, Pest Control, Water, etc. (PM-managed expenses only)

They do NOT include: Mortgage, Insurance, Property Taxes, HOA, Owner Distributions, Security Deposits, etc.

We compare Total Income, Total Expense, and Net Income from PDFs against
equivalent sums from the Transactions table (filtering to PM-relevant categories).
"""

import pdfplumber
import pyodbc
import re
import os
from pathlib import Path
from decimal import Decimal, ROUND_HALF_UP

PDF_DIR = r"C:\SWAM DOCUMENTS\TAXES\2025\Property Manager Statements\Rose Residential"
CONN_STR = "Driver={ODBC Driver 17 for SQL Server};Server=localhost;Database=PropertyManagement;Trusted_Connection=Yes;"

MONTHS = ["Jan 2025","Feb 2025","Mar 2025","Apr 2025","May 2025","Jun 2025",
          "Jul 2025","Aug 2025","Sep 2025","Oct 2025","Nov 2025","Dec 2025"]

# Map PDF filename keywords to PropertyId
# We'll build this dynamically by matching PDF names to DB properties

def parse_amount(s):
    """Parse a formatted number like '1,785.00' or '-292.80' into Decimal."""
    s = s.strip().replace(",", "")
    return Decimal(s)

def extract_pdf_data(pdf_path):
    """Extract Total Income, Total Expense, Net Income monthly arrays + totals from a PDF."""
    with pdfplumber.open(pdf_path) as pdf:
        text = ""
        for page in pdf.pages:
            text += page.extract_text() + "\n"

    result = {
        "property_name": None,
        "total_income": None,   # list of 12 monthly + 1 annual total
        "total_expense": None,
        "net_income": None,
        "income_lines": {},     # line_label -> [12 monthly + total]
        "expense_lines": {},
    }

    # Extract property name from header
    m = re.search(r"Properties:\s*(.+?)(?:\s+(?:San Antonio|Lacey|Columbia))", text)
    if m:
        result["property_name"] = m.group(1).strip()

    lines = text.split("\n")

    def find_row_values(label, lines_list):
        """Find a row by label and extract 13 numeric values (12 months + total)."""
        # Some labels span multiple lines (e.g., "Roof Repairs\nand\nMaintenance")
        # We look for lines containing the label followed by numbers
        for i, line in enumerate(lines_list):
            if label.lower() in line.lower():
                # Collect all numbers from this line
                nums = re.findall(r'-?[\d,]+\.\d{2}', line)
                if len(nums) >= 13:
                    return [parse_amount(n) for n in nums[:13]]
                # Maybe numbers continue on next line
                if i + 1 < len(lines_list):
                    combined = line + " " + lines_list[i + 1]
                    nums = re.findall(r'-?[\d,]+\.\d{2}', combined)
                    if len(nums) >= 13:
                        return [parse_amount(n) for n in nums[:13]]
        return None

    # Extract summary rows - look for the LAST occurrence of each
    # (they appear twice: once in the body, once in the summary at bottom)
    def find_last_row_values(label, lines_list):
        """Find the LAST occurrence of a row by label."""
        result_vals = None
        for i, line in enumerate(lines_list):
            if label.lower() in line.lower():
                nums = re.findall(r'-?[\d,]+\.\d{2}', line)
                if len(nums) >= 13:
                    result_vals = [parse_amount(n) for n in nums[:13]]
                elif i + 1 < len(lines_list):
                    combined = line + " " + lines_list[i + 1]
                    nums = re.findall(r'-?[\d,]+\.\d{2}', combined)
                    if len(nums) >= 13:
                        result_vals = [parse_amount(n) for n in nums[:13]]
        return result_vals

    result["total_income"] = find_last_row_values("Total Income", lines)
    result["total_expense"] = find_last_row_values("Total Expense", lines)
    result["net_income"] = find_last_row_values("Net Income", lines)

    # Also extract individual line items for detailed comparison
    in_income = False
    in_expense = False
    for i, line in enumerate(lines):
        if "Income" == line.strip() and i > 5:
            in_income = True
            in_expense = False
            continue
        if "Expense" == line.strip() and i > 5:
            in_income = False
            in_expense = True
            continue
        if "Total Operating" in line:
            in_income = False
            in_expense = False
            continue

        nums = re.findall(r'-?[\d,]+\.\d{2}', line)
        if len(nums) >= 13:
            # Extract the label (text before the numbers)
            label_match = re.match(r'^(.+?)\s+-?[\d,]+\.\d{2}', line)
            if label_match:
                label = label_match.group(1).strip()
                values = [parse_amount(n) for n in nums[:13]]
                if in_income:
                    result["income_lines"][label] = values
                elif in_expense:
                    result["expense_lines"][label] = values

    return result

def get_db_data(conn):
    """Get transaction data from DB, grouped by property and month.
    
    We only include categories that appear on the PM income statement:
    Income: Rent, Other Income, Pet Fees (positive amounts from any category count as income on PM statement)
    Expense: Management Fees, Repairs & Maintenance, Utilities, Capital Expenses, Other Professional Services
    
    Excluded (not on PM statement): Mortgage/Loan Payment, Insurance, Property Taxes, HOA Dues,
    Owner Distributions, Security Deposits, Transfers
    """
    cursor = conn.cursor()
    
    # Get all transactions for 2025 from Rose Residential with property info
    cursor.execute("""
        SELECT 
            t.PropertyId,
            p.Street,
            p.FullAddress,
            t.TransactionDate,
            t.Category,
            t.SubCategory,
            t.Amount,
            t.Name,
            t.Unit,
            MONTH(t.TransactionDate) as Mo
        FROM Transactions t
        JOIN Properties p ON t.PropertyId = p.PropertyId
        WHERE YEAR(t.TransactionDate) = 2025
          AND t.DataSource = 'Rose Residential LLC'
        ORDER BY t.PropertyId, t.TransactionDate
    """)
    
    rows = cursor.fetchall()
    
    # Group by property
    properties = {}
    for row in rows:
        pid = row.PropertyId
        if pid not in properties:
            properties[pid] = {
                "street": row.Street,
                "full_address": row.FullAddress,
                "transactions": []
            }
        properties[pid]["transactions"].append({
            "date": row.TransactionDate,
            "category": row.Category,
            "sub_category": row.SubCategory,
            "amount": Decimal(str(row.Amount)),
            "payee": row.Name,
            "unit": row.Unit,
            "month": row.Mo
        })
    
    return properties

def categorize_for_pm(category, sub_category, amount):
    """
    Classify a transaction as PM-income, PM-expense, or excluded.
    This matches what Rose Residential tracks on their income statements.
    
    Actual DB categories:
      Income (Rents, Pet Fees, Section 8 Rents, NULL)
      Management Fees, Repairs & Maintenance, Utilities, Capital Expenses,
      Legal & Professional, Admin & Other, Mortgages & Loans, Insurance,
      Taxes, Security Deposits, Transfers
    
    Returns: ('income', amount), ('expense', amount), or ('excluded', 0)
    """
    cat = (category or "").strip()
    
    # PM-managed INCOME
    if cat == "Income":
        return ("income", amount)
    
    # PM-managed EXPENSES
    if cat in ("Management Fees", "Repairs & Maintenance", "Utilities",
               "Capital Expenses", "Legal & Professional", "Admin & Other"):
        return ("expense", amount)
    
    # NOT on PM income statement
    # Mortgages & Loans, Insurance, Taxes, Security Deposits, Transfers, NULL
    return ("excluded", Decimal("0"))

def compute_pm_totals(transactions):
    """Compute monthly income/expense totals matching PM statement logic."""
    income = [Decimal("0")] * 12
    expense = [Decimal("0")] * 12
    
    for txn in transactions:
        month_idx = txn["month"] - 1  # 0-based
        classification, _ = categorize_for_pm(txn["category"], txn["sub_category"], txn["amount"])
        
        if classification == "income":
            income[month_idx] += txn["amount"]
        elif classification == "expense":
            # In DB: negative = expense, positive = credit/refund
            # On PM statement: expenses shown as positive numbers
            # The PM statement shows the absolute value of expenses,
            # but credits reduce the expense amount
            # So we add the amount as-is (negative amounts increase expense, positive decrease)
            expense[month_idx] += txn["amount"]  # We'll negate later
    
    # Convert expense to positive (PM shows expenses as positive)
    # In DB: expenses are negative, so negate to make positive
    expense_positive = [(-e).quantize(Decimal("0.01")) for e in expense]
    income_rounded = [i.quantize(Decimal("0.01")) for i in income]
    
    return income_rounded, expense_positive

def match_pdf_to_property(pdf_name, db_properties):
    """Match a PDF filename to a database property."""
    # Extract the key part of the filename
    # e.g., "income_statement_12_month-2026_Rattler.pdf" -> "Rattler"
    m = re.search(r'in?come_statement_12_month-\d+_(.+)\.pdf', pdf_name)
    if not m:
        return None
    
    pdf_key = m.group(1).strip().lower()
    
    # Build mapping
    mappings = {
        "334 otter": 20,
        "adams hill": 2,
        "amber field": 3,
        "arch brg": 4,
        "ashwood": 5,
        "autumn": 6,
        "bayhorse": 7,
        "beau brg": 8,
        "branston": 9,
        "centerville": 10,
        "donley": 11,
        "emerald": 12,
        "gillcross": 13,
        "hawksbill": 14,
        "jaclyn": 15,
        "longtrail": 16,
        "maddux": 17,
        "meadow field unit 1": 18,  # Meadow Fld is PropertyId 18 with 4 units
        "meadow field unit 2": 18,
        "meadow field unit 3": 18,
        "meadow field unit 4": 18,
        "rattler": 22,
        "saddlebrook": 23,
        "sageline": 24,
    }
    
    return mappings.get(pdf_key)

def main():
    # Connect to DB
    conn = pyodbc.connect(CONN_STR)
    db_data = get_db_data(conn)
    
    # Get list of income statement PDFs
    pdf_files = sorted([
        f for f in os.listdir(PDF_DIR) 
        if (f.startswith("income_statement_") or f.startswith("inincome_statement_")) and f.endswith(".pdf")
        and "20260118" not in f  # Skip the unnamed duplicates
    ])
    
    print("=" * 120)
    print("INCOME STATEMENT VERIFICATION: PDF vs Database")
    print("=" * 120)
    
    all_discrepancies = []
    meadow_field_pdfs = {}  # Collect Meadow Field units for combined comparison
    
    for pdf_file in pdf_files:
        pdf_path = os.path.join(PDF_DIR, pdf_file)
        
        # Check if this is a Meadow Field unit
        mf_match = re.search(r'Meadow Field Unit (\d)', pdf_file, re.IGNORECASE)
        if mf_match:
            unit = int(mf_match.group(1))
            pdf_data = extract_pdf_data(pdf_path)
            meadow_field_pdfs[unit] = pdf_data
            continue
        
        # Also check for the misspelled one
        if "inincome_statement" in pdf_file:
            mf_match2 = re.search(r'Meadow Field Unit (\d)', pdf_file, re.IGNORECASE)
            if mf_match2:
                unit = int(mf_match2.group(1))
                pdf_data = extract_pdf_data(pdf_path)
                meadow_field_pdfs[unit] = pdf_data
                continue
        
        prop_id = match_pdf_to_property(pdf_file, db_data)
        if prop_id is None:
            print(f"\n⚠️  Could not match PDF: {pdf_file}")
            continue
        
        if prop_id not in db_data:
            print(f"\n⚠️  No DB transactions for PropertyId {prop_id} ({pdf_file})")
            continue
        
        pdf_data = extract_pdf_data(pdf_path)
        db_income, db_expense = compute_pm_totals(db_data[prop_id]["transactions"])
        
        street = db_data[prop_id]["street"]
        
        print(f"\n{'─' * 120}")
        print(f"Property: {street} (ID: {prop_id}) | PDF: {pdf_file}")
        print(f"{'─' * 120}")
        
        has_discrepancy = False
        
        # Compare Total Income
        if pdf_data["total_income"]:
            pdf_inc = pdf_data["total_income"]
            print(f"\n  {'TOTAL INCOME':20s} | {'Jan':>9s} {'Feb':>9s} {'Mar':>9s} {'Apr':>9s} {'May':>9s} {'Jun':>9s} {'Jul':>9s} {'Aug':>9s} {'Sep':>9s} {'Oct':>9s} {'Nov':>9s} {'Dec':>9s} | {'TOTAL':>10s}")
            print(f"  {'PDF':20s} |", " ".join(f"{v:>9.2f}" for v in pdf_inc[:12]), f"| {pdf_inc[12]:>10.2f}")
            print(f"  {'Database':20s} |", " ".join(f"{v:>9.2f}" for v in db_income), f"| {sum(db_income):>10.2f}")
            
            diffs_inc = [db_income[i] - pdf_inc[i] for i in range(12)]
            total_diff_inc = sum(db_income) - pdf_inc[12]
            if any(d != 0 for d in diffs_inc) or total_diff_inc != 0:
                print(f"  {'DIFFERENCE':20s} |", " ".join(f"{v:>9.2f}" for v in diffs_inc), f"| {total_diff_inc:>10.2f}")
                has_discrepancy = True
                for i, d in enumerate(diffs_inc):
                    if d != 0:
                        all_discrepancies.append({
                            "property": street,
                            "pid": prop_id,
                            "type": "Income",
                            "month": MONTHS[i],
                            "pdf": float(pdf_inc[i]),
                            "db": float(db_income[i]),
                            "diff": float(d)
                        })
        
        # Compare Total Expense
        if pdf_data["total_expense"]:
            pdf_exp = pdf_data["total_expense"]
            print(f"\n  {'TOTAL EXPENSE':20s} | {'Jan':>9s} {'Feb':>9s} {'Mar':>9s} {'Apr':>9s} {'May':>9s} {'Jun':>9s} {'Jul':>9s} {'Aug':>9s} {'Sep':>9s} {'Oct':>9s} {'Nov':>9s} {'Dec':>9s} | {'TOTAL':>10s}")
            print(f"  {'PDF':20s} |", " ".join(f"{v:>9.2f}" for v in pdf_exp[:12]), f"| {pdf_exp[12]:>10.2f}")
            print(f"  {'Database':20s} |", " ".join(f"{v:>9.2f}" for v in db_expense), f"| {sum(db_expense):>10.2f}")
            
            diffs_exp = [db_expense[i] - pdf_exp[i] for i in range(12)]
            total_diff_exp = sum(db_expense) - pdf_exp[12]
            if any(d != 0 for d in diffs_exp) or total_diff_exp != 0:
                print(f"  {'DIFFERENCE':20s} |", " ".join(f"{v:>9.2f}" for v in diffs_exp), f"| {total_diff_exp:>10.2f}")
                has_discrepancy = True
                for i, d in enumerate(diffs_exp):
                    if d != 0:
                        all_discrepancies.append({
                            "property": street,
                            "pid": prop_id,
                            "type": "Expense",
                            "month": MONTHS[i],
                            "pdf": float(pdf_exp[i]),
                            "db": float(db_expense[i]),
                            "diff": float(d)
                        })
        
        if not has_discrepancy:
            print(f"\n  ✅ MATCH — No discrepancies found")
    
    # Handle Meadow Field (4 units combined in DB as PropertyId 18)
    if meadow_field_pdfs and 18 in db_data:
        print(f"\n{'─' * 120}")
        print(f"Property: 5212 Meadow Fld (ID: 18) — 4 Units Combined | PDFs: Meadow Field Unit 1-4")
        print(f"{'─' * 120}")
        
        # Sum all unit PDFs
        combined_pdf_income = [Decimal("0")] * 13
        combined_pdf_expense = [Decimal("0")] * 13
        
        for unit_num, unit_data in sorted(meadow_field_pdfs.items()):
            if unit_data["total_income"]:
                for i in range(13):
                    combined_pdf_income[i] += unit_data["total_income"][i]
            if unit_data["total_expense"]:
                for i in range(13):
                    combined_pdf_expense[i] += unit_data["total_expense"][i]
        
        db_income, db_expense = compute_pm_totals(db_data[18]["transactions"])
        
        has_discrepancy = False
        
        print(f"\n  {'TOTAL INCOME':20s} | {'Jan':>9s} {'Feb':>9s} {'Mar':>9s} {'Apr':>9s} {'May':>9s} {'Jun':>9s} {'Jul':>9s} {'Aug':>9s} {'Sep':>9s} {'Oct':>9s} {'Nov':>9s} {'Dec':>9s} | {'TOTAL':>10s}")
        print(f"  {'PDF (4 units)':20s} |", " ".join(f"{v:>9.2f}" for v in combined_pdf_income[:12]), f"| {combined_pdf_income[12]:>10.2f}")
        print(f"  {'Database':20s} |", " ".join(f"{v:>9.2f}" for v in db_income), f"| {sum(db_income):>10.2f}")
        
        diffs_inc = [db_income[i] - combined_pdf_income[i] for i in range(12)]
        total_diff_inc = sum(db_income) - combined_pdf_income[12]
        if any(d != 0 for d in diffs_inc) or total_diff_inc != 0:
            print(f"  {'DIFFERENCE':20s} |", " ".join(f"{v:>9.2f}" for v in diffs_inc), f"| {total_diff_inc:>10.2f}")
            has_discrepancy = True
            for i, d in enumerate(diffs_inc):
                if d != 0:
                    all_discrepancies.append({
                        "property": "5212 Meadow Fld",
                        "pid": 18,
                        "type": "Income",
                        "month": MONTHS[i],
                        "pdf": float(combined_pdf_income[i]),
                        "db": float(db_income[i]),
                        "diff": float(d)
                    })
        
        print(f"\n  {'TOTAL EXPENSE':20s} | {'Jan':>9s} {'Feb':>9s} {'Mar':>9s} {'Apr':>9s} {'May':>9s} {'Jun':>9s} {'Jul':>9s} {'Aug':>9s} {'Sep':>9s} {'Oct':>9s} {'Nov':>9s} {'Dec':>9s} | {'TOTAL':>10s}")
        print(f"  {'PDF (4 units)':20s} |", " ".join(f"{v:>9.2f}" for v in combined_pdf_expense[:12]), f"| {combined_pdf_expense[12]:>10.2f}")
        print(f"  {'Database':20s} |", " ".join(f"{v:>9.2f}" for v in db_expense), f"| {sum(db_expense):>10.2f}")
        
        diffs_exp = [db_expense[i] - combined_pdf_expense[i] for i in range(12)]
        total_diff_exp = sum(db_expense) - combined_pdf_expense[12]
        if any(d != 0 for d in diffs_exp) or total_diff_exp != 0:
            print(f"  {'DIFFERENCE':20s} |", " ".join(f"{v:>9.2f}" for v in diffs_exp), f"| {total_diff_exp:>10.2f}")
            has_discrepancy = True
            for i, d in enumerate(diffs_exp):
                if d != 0:
                    all_discrepancies.append({
                        "property": "5212 Meadow Fld",
                        "pid": 18,
                        "type": "Expense",
                        "month": MONTHS[i],
                        "pdf": float(combined_pdf_expense[i]),
                        "db": float(db_expense[i]),
                        "diff": float(d)
                    })
        
        if not has_discrepancy:
            print(f"\n  ✅ MATCH — No discrepancies found")
    
    # Summary
    print(f"\n{'=' * 120}")
    print(f"DISCREPANCY SUMMARY")
    print(f"{'=' * 120}")
    
    if not all_discrepancies:
        print("\n  ✅ ALL PROPERTIES MATCH — No discrepancies found between PDF income statements and database!")
    else:
        print(f"\n  Found {len(all_discrepancies)} discrepancies:\n")
        print(f"  {'Property':<25s} {'Type':<10s} {'Month':<12s} {'PDF':>12s} {'Database':>12s} {'Diff':>12s}")
        print(f"  {'─'*25} {'─'*10} {'─'*12} {'─'*12} {'─'*12} {'─'*12}")
        for d in all_discrepancies:
            print(f"  {d['property']:<25s} {d['type']:<10s} {d['month']:<12s} {d['pdf']:>12.2f} {d['db']:>12.2f} {d['diff']:>12.2f}")
        
        total_income_diff = sum(d["diff"] for d in all_discrepancies if d["type"] == "Income")
        total_expense_diff = sum(d["diff"] for d in all_discrepancies if d["type"] == "Expense")
        print(f"\n  Total Income Discrepancy:  ${total_income_diff:>12.2f}")
        print(f"  Total Expense Discrepancy: ${total_expense_diff:>12.2f}")
    
    conn.close()

if __name__ == "__main__":
    main()
