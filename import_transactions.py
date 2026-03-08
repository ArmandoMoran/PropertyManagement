import csv
import pyodbc
from datetime import datetime

conn_str = r'DRIVER={ODBC Driver 17 for SQL Server};SERVER=localhost;DATABASE=PropertyManagement;Trusted_Connection=yes;'
csv_path = r'C:\Users\arman\Downloads\Transactions.csv'

# Mapping from CSV Property values to PropertyId
# CSV Property -> PropertyId
PROPERTY_MAP = {
    '10010 Amber Field Dr': 3,
    '11519 Long Trail': 16,
    '1226 Bay Horse Dr': 7,
    '1508-1510 Raleigh Dr': 21,
    '2118 Centerville Dr': 10,
    '334 Otter Dr': 20,
    '514 Maddux St': 17,
    '5212 Meadow Field': 18,
    '539 Rattler Bluff': 22,
    '6528 Oklahoma St SE': 19,
    '7714 Branston': 9,
    '7935 Emerald Elm': 12,
    '8023 Ashwood Pointe': 5,
    '8215 Donley Pond': 11,
    '8322 Sageline St': 24,
    '838 Saddlebrook Dr': 23,
    '8411 Jaclyn Park': 15,
    '8815 Adams Hill Dr': 2,
    '8914 Arch Bridge': 4,
    '9330 Gillcross Way': 13,
    '9535 Beau Bridge': 8,
    '9558 Autumn Shade': 6,
    '9967 Hawksbill Peak': 14,
}

# Also try to match from transaction Name field when Property column is empty
# These are common patterns in bill pay names
NAME_KEYWORDS = {
    'Arch Bridge': 4,
    'CENTERVILLE': 10,
    'AUTUMN SHADE': 6,
    'Rattler': 22,
    'GILLCROSS': 13,
    'Jaclyn Park': 15,
    'BRANSTON': 9,
    'Ashwood Pointe': 5,
    'DONLEY POND': 11,
    'HAWKSBILL': 14,
    'SADDLEBROOK': 23,
    'Beau Br': 8,
    'EMERALD ELM': 12,
    'AMBER FIELD': 3,
    'BAY HORSE': 7,
    'LONG TRAIL': 16,
    'MADDUX': 17,
    'MEADOW': 18,
    'OTTER': 20,
    'RALEIGH': 21,
    'SAGELINE': 24,
    'OKLAHOMA': 19,
    'Adam Hill': 2,
    'Adams Hill': 2,
}


def find_property_id(property_val, name_val):
    """Try to match property from the Property column first, then from Name."""
    prop = (property_val or '').strip()
    if prop and prop in PROPERTY_MAP:
        return PROPERTY_MAP[prop]

    # Try fuzzy match on Property column
    if prop:
        prop_upper = prop.upper()
        for key, pid in PROPERTY_MAP.items():
            if key.upper() in prop_upper or prop_upper in key.upper():
                return pid

    # Try matching from Name column
    name = (name_val or '').strip().upper()
    if name:
        for keyword, pid in NAME_KEYWORDS.items():
            if keyword.upper() in name:
                return pid

    return None


def parse_date(date_str):
    """Parse MM/DD/YYYY date string."""
    try:
        return datetime.strptime(date_str.strip(), '%m/%d/%Y').date()
    except (ValueError, AttributeError):
        return None


def parse_amount(amount_str):
    """Parse amount string, removing commas."""
    try:
        return float(amount_str.strip().replace(',', ''))
    except (ValueError, AttributeError):
        return None


def main():
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()

    insert_sql = """
        INSERT INTO Transactions 
        (TransactionDate, Name, Notes, Details, Category, SubCategory, Amount,
         Portfolio, PropertyId, PropertyRaw, Unit, DataSource, Account, Owner, Attachments)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    """

    total = 0
    matched = 0
    unmatched = 0
    errors = 0

    with open(csv_path, 'r', encoding='utf-8-sig') as f:
        reader = csv.DictReader(f)
        for row in reader:
            total += 1
            try:
                tx_date = parse_date(row.get('Date', ''))
                name = (row.get('Name', '') or '')[:500]
                notes = (row.get('Notes', '') or '')[:1000]
                details = (row.get('Details', '') or '')[:500]
                category = (row.get('Category', '') or '')[:100]
                sub_category = (row.get('Sub-Category', '') or '')[:100]
                amount = parse_amount(row.get('Amount', ''))
                portfolio = (row.get('Portfolio', '') or '')[:100]
                property_raw = (row.get('Property', '') or '')[:200]
                unit = (row.get('Unit', '') or '')[:50]
                data_source = (row.get('Data Source', '') or '')[:100]
                account = (row.get('Account', '') or '')[:200]
                owner = (row.get('Owner', '') or '')[:200]
                attachments = (row.get('Attachments', '') or '')[:500]

                property_id = find_property_id(property_raw, name)

                if property_id:
                    matched += 1
                else:
                    unmatched += 1

                cursor.execute(insert_sql, (
                    tx_date, name or None, notes or None, details or None,
                    category or None, sub_category or None, amount,
                    portfolio or None, property_id, property_raw or None,
                    unit or None, data_source or None, account or None,
                    owner or None, attachments or None
                ))

            except Exception as e:
                errors += 1
                print(f"Error on row {total}: {e}")
                print(f"  Row data: {row}")

    conn.commit()
    cursor.close()
    conn.close()

    print(f"\n=== Import Complete ===")
    print(f"Total rows:    {total}")
    print(f"Matched:       {matched} (linked to a property)")
    print(f"Unmatched:     {unmatched} (no property link)")
    print(f"Errors:        {errors}")


if __name__ == '__main__':
    main()
