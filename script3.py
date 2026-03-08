import sys

path = r'C:\repos\src\PropertyManagement.Infrastructure\Excel\ExcelReportGenerator.cs'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

reps = [
    (
        '''        // Escrow
        ws.Cell(row, 1).Value = "Escrow";
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.DarkBlue;
        for (int m = 0; m < 12; m++)
        {
            ws.Cell(row, m + 2).Value = report.EscrowPayments[m];
            ws.Cell(row, m + 2).Style.NumberFormat.Format = "#,##0.00";
        }
        ws.Cell(row, 14).Value = report.EscrowPayments.Sum();
        ws.Cell(row, 14).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 14).Style.Font.Bold = true;''',
        '''        // Escrow
        rowMap["Escrow Payments"] = row;
        ws.Cell(row, 1).Value = "Escrow";
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.DarkBlue;
        for (int m = 0; m < 12; m++)
        {
            ws.Cell(row, m + 2).Value = report.EscrowPayments[m];
            ws.Cell(row, m + 2).Style.NumberFormat.Format = "#,##0.00";
        }
        ws.Cell(row, 14).Value = report.EscrowPayments.Sum();
        ws.Cell(row, 14).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 14).Style.Font.Bold = true;'''
    ),
]

for o, n in reps:
    if o in text:
        text = text.replace(o, n)
    else:
        print("Missing:", o[:50])

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

print("Patch complete.")
