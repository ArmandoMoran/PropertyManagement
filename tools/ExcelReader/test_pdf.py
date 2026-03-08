import pdfplumber
import sys
pdf_path = r"c:\SWAM DOCUMENTS\TAXES\2025\1098\Ashwood Pointe 1098 -0927-.pdf"
try:
    with pdfplumber.open(pdf_path) as pdf:
        text = "\n".join(page.extract_text() for page in pdf.pages if page.extract_text())
        print(f"Extracted {len(text)} characters")
        print(text[:1000])
except Exception as e:
    print("Error:", e)
