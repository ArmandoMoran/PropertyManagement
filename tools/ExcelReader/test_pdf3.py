import pdfplumber
import sys
import os

pdfs = [
"c:\\SWAM DOCUMENTS\\TAXES\\2025\\1098\\Ashwood Pointe 1098 -0927-.pdf",
"c:\\SWAM DOCUMENTS\\TAXES\\2025\\1098\\Autumn Shade 1098 RoundPoint 1 Pg.pdf",
"c:\\SWAM DOCUMENTS\\TAXES\\2025\\1098\\BAY HORSE 1098.pdf",
"c:\\SWAM DOCUMENTS\\TAXES\\2025\\1098\\BEAU BRIDGE 1098 - Chase.pdf",
"c:\\SWAM DOCUMENTS\\TAXES\\2025\\1098\\Centerville 1098-MORT MORTGAGE 4112 WellsFargo.pdf",
"c:\\SWAM DOCUMENTS\\TAXES\\2025\\1098\\Donley Pond 1098-MORT MORTGAGE 9591 WellsFargo.pdf",
"c:\\SWAM DOCUMENTS\\TAXES\\2025\\1098\\Emerald Elm 1098.pdf",
"c:\\SWAM DOCUMENTS\\TAXES\\2025\\1098\\Gillcross 1098.pdf",
"c:\\SWAM DOCUMENTS\\TAXES\\2025\\1098\\Jaclyn Park 1098.pdf",
"c:\\SWAM DOCUMENTS\\TAXES\\2025\\1098\\MEADOW FIELD 1098 RoundPoint 1 Pg.pdf",
"c:\\SWAM DOCUMENTS\\TAXES\\2025\\1098\\Oklahoma 1098 Shellpoint.pdf",
"c:\\SWAM DOCUMENTS\\TAXES\\2025\\1098\\Raleigh 1098.pdf",
"c:\\SWAM DOCUMENTS\\TAXES\\2025\\1098\\Rattler Bluff 1098-MORT MORTGAGE 2153 WellsFargo.pdf",
"c:\\SWAM DOCUMENTS\\TAXES\\2025\\1098\\Arch Bridge 1098.pdf"
]

for p in pdfs:
    print(f"--- {os.path.basename(p)} ---")
    try:
        with pdfplumber.open(p) as pdf:
            text = pdf.pages[0].extract_text()
            if text:
                for line in text.split('\n'):
                    if 'Mortgage interest' in line or 'Box 1' in line or '1 ' in line or '$' in line:
                        print(line.strip()[:100])
    except Exception as e:
        print(e)
