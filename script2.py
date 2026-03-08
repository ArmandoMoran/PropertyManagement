import sys

path = r'C:\repos\src\PropertyManagement.Infrastructure\Excel\ExcelReportGenerator.cs'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

reps = [
    (
        '''        // === ROW 15: Management Fee ===
        row = 15;
        WriteMonthlyRow(ws, row, "Management Fee", report.ManagementFee);''',
        '''        // === ROW 15: Management Fee ===
        row = 15;
        WriteMonthlyRow(ws, row, "Management Fee", report.ManagementFee);
        rowMap["Management Fee"] = row;'''
    ),
    (
        '''        // === ROW 16: Other Professional Services ===
        row = 16;
        if (report.OtherProfessionalServices.Any(v => v != 0) || report.LeasingCommissions.Any(v => v != 0))
        {
            decimal[] combined = new decimal[12];
            for (int i = 0; i < 12; i++)
                combined[i] = report.OtherProfessionalServices[i] + report.LeasingCommissions[i];
            WriteMonthlyRow(ws, row, "Other Professional Svc", combined);
        }''',
        '''        // === ROW 16: Other Professional Services ===
        row = 16;
        if (report.OtherProfessionalServices.Any(v => v != 0) || report.LeasingCommissions.Any(v => v != 0))
        {
            rowMap["Other Professional Services"] = row;
            decimal[] combined = new decimal[12];
            for (int i = 0; i < 12; i++)
                combined[i] = report.OtherProfessionalServices[i] + report.LeasingCommissions[i];
            WriteMonthlyRow(ws, row, "Other Professional Svc", combined);
        }'''
    ),
    (
        '''        // === ROW 18: Property Taxes ===
        row = 18;
        WriteMonthlyRow(ws, row, "Property Taxes", report.PropertyTaxes);''',
        '''        // === ROW 18: Property Taxes ===
        row = 18;
        WriteMonthlyRow(ws, row, "Property Taxes", report.PropertyTaxes);
        rowMap["Property Taxes"] = row;'''
    ),
    (
        '''        // === ROW 19: Insurance ===
        row = 19;
        WriteMonthlyRow(ws, row, "Insurance", report.InsurancePremium);''',
        '''        // === ROW 19: Insurance ===
        row = 19;
        WriteMonthlyRow(ws, row, "Insurance", report.InsurancePremium);
        rowMap["Insurance"] = row;'''
    ),
    (
        '''        // === ROW 20: HOA ===
        row = 20;
        WriteMonthlyRow(ws, row, "HOA", report.HoaDues);''',
        '''        // === ROW 20: HOA ===
        row = 20;
        WriteMonthlyRow(ws, row, "HOA", report.HoaDues);
        rowMap["HOA"] = row;'''
    ),
    (
        '''        // Escrow
        ws.Cell(row, 1).Value = "Escrow Payment";
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
        ws.Cell(row, 1).Value = "Escrow Payment";
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
