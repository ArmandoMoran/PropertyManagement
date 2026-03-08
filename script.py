import sys
import re

path = r'C:\repos\src\PropertyManagement.Infrastructure\Excel\ExcelReportGenerator.cs'

with open(path, 'r', encoding='utf-8') as f:
    code = f.read()


# 1. Update row maps correctly in BuildPropertySheet
replacements = [
    # Mortgage Interest
    (
        '''        // === ROW 17: Mortgage Interest ===
        row = 17;
        WriteMonthlyRow(ws, row, "Mortgage Interest", report.MortgageInterest);''',
        '''        // === ROW 17: Mortgage Interest ===
        row = 17;
        WriteMonthlyRow(ws, row, "Mortgage Interest", report.MortgageInterest);
        rowMap["Mortgage Interest"] = row;

        if (report.InterestPaid1098 > 0)
        {
            ws.Cell(row, 17).Value = report.InterestPaid1098;
            ws.Cell(row, 17).Style.NumberFormat.Format = "_($* #,##0.00_);_($* (#,##0.00);_($* \\"-\\\"??_);_(@_)";
            ws.Cell(row, 18).Value = "1098";
        }'''
    ),
    # WriteMonthlyRow mapping for simple rows (like Rent, etc. if needed)
    (
        '''        // === ROW 7: Rent ===
        row = 7;
        WriteMonthlyRow(ws, row, "Rent", report.Rent);''',
        '''        // === ROW 7: Rent ===
        row = 7;
        WriteMonthlyRow(ws, row, "Rent", report.Rent);
        rowMap["Rent"] = row;'''
    ),
    (
        '''        // === ROW 12: Total Income ===
        row = 12;
        ws.Cell(row, 1).Value = "Total Income";''',
        '''        // === ROW 12: Total Income ===
        row = 12;
        rowMap["Total Income"] = row;
        ws.Cell(row, 1).Value = "Total Income";'''
    ),
    (
        '''        WriteMonthlyRow(ws, row, "Management Fee", report.ManagementFee);
        row++;

        WriteMonthlyRow(ws, row, "Leasing Commissions", report.LeasingCommissions);
        row++;

        WriteMonthlyRow(ws, row, "Other Professional Services", report.OtherProfessionalServices);''',
        '''        WriteMonthlyRow(ws, row, "Management Fee", report.ManagementFee);
        rowMap["Management Fee"] = row;
        row++;

        WriteMonthlyRow(ws, row, "Leasing Commissions", report.LeasingCommissions);
        rowMap["Leasing Commissions"] = row;
        row++;

        WriteMonthlyRow(ws, row, "Other Professional Services", report.OtherProfessionalServices);
        rowMap["Other Professional Services"] = row;'''
    ),
    (
        '''        WriteMonthlyRow(ws, row, "Property Taxes", report.PropertyTaxes);
        row++;

        WriteMonthlyRow(ws, row, "Insurance Premium", report.InsurancePremium);
        row++;

        WriteMonthlyRow(ws, row, "HOA Dues", report.HoaDues);''',
        '''        WriteMonthlyRow(ws, row, "Property Taxes", report.PropertyTaxes);
        rowMap["Property Taxes"] = row;
        row++;

        WriteMonthlyRow(ws, row, "Insurance", report.InsurancePremium);
        rowMap["Insurance"] = row;
        row++;

        WriteMonthlyRow(ws, row, "HOA", report.HoaDues);
        rowMap["HOA"] = row;'''
    ),
    (
        '''        // === ROWS 22+: Dynamic repair lines ===
        row = 22;
        foreach (var repairLine in report.RepairLines.OrderBy(r => r.SubCategory))
        {
            WriteMonthlyRow(ws, row, $"  {repairLine.SubCategory}", repairLine.MonthlyAmounts);
            row++;
        }''',
        '''        // === ROWS 22+: Dynamic repair lines ===
        rowMap["RepairsStart"] = row;
        foreach (var repairLine in report.RepairLines.OrderBy(r => r.SubCategory))
        {
            WriteMonthlyRow(ws, row, $"  {repairLine.SubCategory}", repairLine.MonthlyAmounts);
            row++;
        }
        rowMap["RepairsEnd"] = Math.Max(rowMap["RepairsStart"], row - 1);'''
    ),
    (
        '''        // Utilities
        if (report.Utilities.Any(v => v != 0))
        {
            WriteMonthlyRow(ws, row, "Utilities", report.Utilities);
            row++;
        }''',
        '''        // Utilities
        rowMap["Utilities"] = row;
        if (report.Utilities.Any(v => v != 0))
        {
            WriteMonthlyRow(ws, row, "Utilities", report.Utilities);
            row++;
        }'''
    ),
    (
        '''        // Capital Expenses
        if (report.CapitalExpenses.Count > 0)
        {
            ws.Cell(row, 1).Value = "Capital Expenses";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.Italic = true;
            row++;

            foreach (var capLine in report.CapitalExpenses.OrderBy(c => c.SubCategory))
            {
                WriteMonthlyRow(ws, row, $"  {capLine.SubCategory}", capLine.MonthlyAmounts);
                row++;
            }
        }''',
        '''        // Capital Expenses
        rowMap["CapExStart"] = row;
        if (report.CapitalExpenses.Count > 0)
        {
            ws.Cell(row, 1).Value = "Capital Expenses";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.Italic = true;
            row++;

            rowMap["CapExStart"] = row;
            foreach (var capLine in report.CapitalExpenses.OrderBy(c => c.SubCategory))
            {
                WriteMonthlyRow(ws, row, $"  {capLine.SubCategory}", capLine.MonthlyAmounts);
                row++;
            }
        }
        rowMap["CapExEnd"] = Math.Max(rowMap["CapExStart"], row - 1);'''
    ),
    (
        '''        // === Total Expenses Row ===
        row++; // blank row
        int totalExpenseRow = row;
        ws.Cell(row, 1).Value = "Total Expenses";''',
        '''        // === Total Expenses Row ===
        row++; // blank row
        int totalExpenseRow = row;
        rowMap["Total Expenses"] = row;
        ws.Cell(row, 1).Value = "Total Expenses";'''
    ),
    (
        '''        // === Net Income Row ===
        row += 2;
        int netIncomeRow = row;
        ws.Cell(row, 1).Value = "Net Income";''',
        '''        // === Net Income Row ===
        row += 2;
        int netIncomeRow = row;
        rowMap["Net Income"] = row;
        ws.Cell(row, 1).Value = "Net Income";'''
    ),
    (
        '''        // Principal Payment
        ws.Cell(row, 1).Value = "Principal Payment";''',
        '''        // Principal Payment
        rowMap["Mortgage Principal"] = row;
        ws.Cell(row, 1).Value = "Principal Payment";'''
    ),
    (
        '''        // Escrow
        ws.Cell(row, 1).Value = "Escrow Payment";''',
        '''        // Escrow
        rowMap["Escrow Payments"] = row;
        ws.Cell(row, 1).Value = "Escrow Payment";'''
    ),
    (
        '''        ws.Cell(row, 1).Value = "NOI";
        ws.Cell(row, 1).Style.Font.Bold = true;''',
        '''        ws.Cell(row, 1).Value = "NOI";
        rowMap["NOI"] = row;
        ws.Cell(row, 1).Style.Font.Bold = true;'''
    ),
]

for old_str, new_str in replacements:
    if old_str in code:
        code = code.replace(old_str, new_str)
    else:
        print(f"Warning: Could not find snippet starting with: {old_str[:50]}")


# 2. BuildSummarySheet Complete Rewrite
b_sum_pattern = re.compile(r'    private void BuildSummarySheet.*?FreezeColumns\(1\);\s*\}', re.DOTALL)

new_bsum_code = '''    private void BuildSummarySheet(XLWorkbook workbook, List<PropertyReportDto> reports, int year)
    {
        var ws = workbook.Worksheets.Add("SUMMARY");
        ws.SetPosition(1); // SUMMARY PAGE MUST BE FIRST
        ws.Column(1).Width = 35; // Wider for row labels

        // Headers
        ws.Cell(1, 1).Value = $"SUMMARY - {year}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        int col = 2;
        foreach (var r in reports)
        {
            var sheetName = r.SheetName.Length > 31 ? r.SheetName[..31] : r.SheetName;
            ws.Cell(2, col).Value = sheetName;
            ws.Cell(2, col).SetHyperlink(new XLHyperlink($"\\'{sheetName}\\'!A1"));
            ws.Cell(2, col).Style.Font.Bold = true;
            ws.Cell(2, col).Style.Font.FontColor = XLColor.Blue;
            ws.Cell(2, col).Style.Font.Underline = XLFontUnderlineValues.Single;
            ws.Column(col).Width = TotalCol_Width;
            col++;
        }
        ws.Cell(2, col).Value = "TOTAL";
        ws.Cell(2, col).Style.Font.Bold = true;
        ws.Column(col).Width = TotalCol_Width;

        int row = 3;

        void AddRowFormula(string label, string rowMapKey, bool isHeader = false, bool isSubtotal = false, bool sumMultiple = false)
        {
            ws.Cell(row, 1).Value = label;
            if (isHeader)
            {
                ws.Cell(row, 1).Style.Font.Bold = true;
                row++;
                return;
            }

            if (isSubtotal)
            {
                ws.Cell(row, 1).Style.Font.Bold = true;
            }

            int c = 2;
            List<string> rowCells = new();
            foreach (var r in reports)
            {
                var sheetName = r.SheetName.Length > 31 ? r.SheetName[..31] : r.SheetName;
                if (!string.IsNullOrEmpty(rowMapKey) && _propertyRowMap.ContainsKey(sheetName) && _propertyRowMap[sheetName].ContainsKey(rowMapKey))
                {
                    int targetRow = _propertyRowMap[sheetName][rowMapKey];
                    // Normal reference
                    ws.Cell(row, c).FormulaA1 = $"=\\'{sheetName}\\'!N{targetRow}";
                }
                else if (sumMultiple && _propertyRowMap.ContainsKey(sheetName))
                {
                    // For things like Repairs where it is a range (RepairsStart -> RepairsEnd)
                    if (rowMapKey == "Repairs" && _propertyRowMap[sheetName].ContainsKey("RepairsStart"))
                    {
                        var start = _propertyRowMap[sheetName]["RepairsStart"];
                        var end = _propertyRowMap[sheetName]["RepairsEnd"];
                        if (end >= start)
                            ws.Cell(row, c).FormulaA1 = $"=SUM(\\'{sheetName}\\'!N{start}:N{end})";
                        else
                            ws.Cell(row, c).Value = 0;
                    }
                    else if (rowMapKey == "CapEx" && _propertyRowMap[sheetName].ContainsKey("CapExStart"))
                    {
                        var start = _propertyRowMap[sheetName]["CapExStart"];
                        var end = _propertyRowMap[sheetName]["CapExEnd"];
                        if (end >= start)
                            ws.Cell(row, c).FormulaA1 = $"=SUM(\\'{sheetName}\\'!N{start}:N{end})";
                        else
                            ws.Cell(row, c).Value = 0;
                    }
                    else 
                    {
                         ws.Cell(row, c).Value = 0;
                    }
                }
                else
                {
                    ws.Cell(row, c).Value = 0;
                }

                ws.Cell(row, c).Style.NumberFormat.Format = "_($* #,##0.00_);_($* (#,##0.00);_($* \\"-\\\"??_);_(@_)";
                if (isSubtotal) ws.Cell(row, c).Style.Font.Bold = true;
                
                var colLetter = ws.Cell(row, c).WorksheetColumn().ColumnLetter();
                rowCells.Add(colLetter + row);
                c++;
            }
            
            // Total Column
            if (rowCells.Count > 0)
            {
                ws.Cell(row, c).FormulaA1 = $"=SUM({rowCells.First()}:{rowCells.Last()})";
            }
            else 
            {
                ws.Cell(row, c).Value = 0;
            }
            
            ws.Cell(row, c).Style.NumberFormat.Format = "_($* #,##0.00_);_($* (#,##0.00);_($* \\"-\\\"??_);_(@_)";
            if (isSubtotal) ws.Cell(row, c).Style.Font.Bold = true;
            row++;
        }

        row++;
        AddRowFormula("INCOME", null, true);
        AddRowFormula("Total Income", "Total Income");

        row++;
        AddRowFormula("EXPENSES", null, true);
        AddRowFormula("Management Fee", "Management Fee");
        AddRowFormula("Leasing Commissions", "Leasing Commissions");
        AddRowFormula("Other Professional Services", "Other Professional Services");
        AddRowFormula("Property Taxes", "Property Taxes");
        AddRowFormula("Insurance", "Insurance");
        AddRowFormula("HOA", "HOA");
        AddRowFormula("Utilities", "Utilities");
        AddRowFormula("Repairs", "Repairs", false, false, true);
        AddRowFormula("Capital Expenses", "CapEx", false, false, true);
        
        // Total Operating Expenses Formula Custom
        int opRow = row;
        ws.Cell(opRow, 1).Value = "Total Operating Expenses";
        ws.Cell(opRow, 1).Style.Font.Bold = true;
        for (int i = 2; i <= reports.Count + 2; i++) {
            var colLetter = ws.Cell(opRow, i).WorksheetColumn().ColumnLetter();
            ws.Cell(opRow, i).FormulaA1 = $"=SUM({colLetter}{opRow-9}:{colLetter}{opRow-1})";
            ws.Cell(opRow, i).Style.NumberFormat.Format = "_($* #,##0.00_);_($* (#,##0.00);_($* \\"-\\\"??_);_(@_)";
            ws.Cell(opRow, i).Style.Font.Bold = true;
        }
        row++;
        
        row++;
        AddRowFormula("NET PERFORMANCE", null, true);
        AddRowFormula("NOI (income - expenses not inc interest)", "NOI", false, true);
        AddRowFormula("Mortgage Interest", "Mortgage Interest");
        AddRowFormula("NOI (income - expenses inc interest)", "Net Income", false, true);
        
        row++;
        AddRowFormula("BELOW THE LINE", null, true);
        AddRowFormula("Mortgage Principal", "Mortgage Principal");
        AddRowFormula("Escrow Payments", "Escrow Payments");

        row++;
        // Cash Flow = NOI (inc interest) - (Principal + Escrow)
        int cfRow = row;
        ws.Cell(cfRow, 1).Value = "Cash Flow";
        ws.Cell(cfRow, 1).Style.Font.Bold = true;
        for (int i = 2; i <= reports.Count + 2; i++) {
            var colLetter = ws.Cell(cfRow, i).WorksheetColumn().ColumnLetter();
            ws.Cell(cfRow, i).FormulaA1 = $"={colLetter}{opRow+4}-({colLetter}{opRow+7}+{colLetter}{opRow+8})";
            ws.Cell(cfRow, i).Style.NumberFormat.Format = "_($* #,##0.00_);_($* (#,##0.00);_($* \\"-\\\"??_);_(@_)";
            ws.Cell(cfRow, i).Style.Font.Bold = true;
        }
        row++;

        ws.SheetView.FreezeRows(2);
        ws.SheetView.FreezeColumns(1);
    }'''

code = b_sum_pattern.sub(new_bsum_code, code)

with open(path, 'w', encoding='utf-8') as f:
    f.write(code)

print("Done. Saved.")
