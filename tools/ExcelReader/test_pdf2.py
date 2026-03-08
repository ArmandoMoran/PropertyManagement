import pdfplumber
import sys
pdf_path = r"c:\SWAM DOCUMENTS\TAXES\2025\1098\Ashwood Pointe 1098 -0927-.pdf"
with pdfplumber.open(pdf_path) as pdf:
    for page in pdf.pages:
        text = page.extract_text()
        if text:
            for line in text.split('\n'):
                if '$' in line or 'Box 1' in line or '1 ' in line or 'Interest' in line:
                    print(line.strip())
