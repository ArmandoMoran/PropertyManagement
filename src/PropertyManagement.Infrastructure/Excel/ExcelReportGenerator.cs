using System.Text.RegularExpressions;
using ClosedXML.Excel;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Interfaces;

namespace PropertyManagement.Infrastructure.Excel;

public class ExcelReportGenerator : IExcelReportGenerator
{
    private static readonly string[] MonthHeaders = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    // Style constants
    private const double ColA_Width = 28;
    private const double MonthCol_Width = 12;
    private const double TotalCol_Width = 14;

    // Holds tracked row numbers for each property to build formulas
    private readonly Dictionary<string, Dictionary<string, int>> _propertyRowMap = new();

    public Task<byte[]> GenerateWorkbookAsync(List<PropertyReportDto> reports, int year)
    {
        using var workbook = new XLWorkbook();

        var orderedReports = reports.OrderBy(r => StreetNameWithoutNumber(r.SheetName), StringComparer.OrdinalIgnoreCase).ToList();

        _propertyRowMap.Clear();
        foreach (var report in orderedReports)
        {
            BuildPropertySheet(workbook, report, year);
        }

        BuildSummarySheet(workbook, orderedReports, year);

        // Move SUMMARY to first position after all sheets exist
        workbook.Worksheet("SUMMARY").Position = 1;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }

    private void BuildSummarySheet(XLWorkbook workbook, List<PropertyReportDto> reports, int year)
    {
        var ws = workbook.Worksheets.Add("SUMMARY");
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
            ws.Cell(2, col).SetHyperlink(new XLHyperlink($"\'{sheetName}\'!A1"));
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
                    ws.Cell(row, c).FormulaA1 = $"=\'{sheetName}\'!N{targetRow}";
                    ws.Cell(row, c).SetHyperlink(new XLHyperlink($"\'{sheetName}\'!N{targetRow}"));
                    ws.Cell(row, c).Style.Font.FontColor = XLColor.Blue;
                }
                else if (sumMultiple && _propertyRowMap.ContainsKey(sheetName))
                {
                    // For things like Repairs where it is a range (RepairsStart -> RepairsEnd)
                    if (rowMapKey == "Repairs" && _propertyRowMap[sheetName].ContainsKey("RepairsStart"))
                    {
                        var start = _propertyRowMap[sheetName]["RepairsStart"];
                        var end = _propertyRowMap[sheetName]["RepairsEnd"];
                        if (end >= start)
                        {
                            ws.Cell(row, c).FormulaA1 = $"=SUM(\'{sheetName}\'!N{start}:N{end})";
                            ws.Cell(row, c).SetHyperlink(new XLHyperlink($"\'{sheetName}\'!N{start}"));
                            ws.Cell(row, c).Style.Font.FontColor = XLColor.Blue;
                        }
                        else
                            ws.Cell(row, c).Value = 0;
                    }
                    else if (rowMapKey == "CapEx" && _propertyRowMap[sheetName].ContainsKey("CapExStart"))
                    {
                        var start = _propertyRowMap[sheetName]["CapExStart"];
                        var end = _propertyRowMap[sheetName]["CapExEnd"];
                        if (end >= start)
                        {
                            ws.Cell(row, c).FormulaA1 = $"=SUM(\'{sheetName}\'!N{start}:N{end})";
                            ws.Cell(row, c).SetHyperlink(new XLHyperlink($"\'{sheetName}\'!N{start}"));
                            ws.Cell(row, c).Style.Font.FontColor = XLColor.Blue;
                        }
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

                ws.Cell(row, c).Style.NumberFormat.Format = "_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)";
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
            
            ws.Cell(row, c).Style.NumberFormat.Format = "_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)";
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
            ws.Cell(opRow, i).Style.NumberFormat.Format = "_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)";
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
            ws.Cell(cfRow, i).Style.NumberFormat.Format = "_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)";
            ws.Cell(cfRow, i).Style.Font.Bold = true;
        }
        row++;

        ws.SheetView.FreezeRows(2);
        ws.SheetView.FreezeColumns(1);
    }

    private void BuildPropertySheet(XLWorkbook workbook, PropertyReportDto report, int year)
    {
        // Sheet name max 31 chars
        var sheetName = report.SheetName.Length > 31
            ? report.SheetName[..31]
            : report.SheetName;

        var rowMap = new Dictionary<string, int>();
        _propertyRowMap[sheetName] = rowMap;

        var ws = workbook.Worksheets.Add(sheetName);

        // Column widths
        ws.Column(1).Width = ColA_Width;
        for (int c = 2; c <= 13; c++) ws.Column(c).Width = MonthCol_Width;
        ws.Column(14).Width = TotalCol_Width;

        int row = 1;

        // === ROW 1: Header ===
        ws.Cell(row, 1).Value = $"Rental Profit & Loss - {year}";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;

        ws.Cell(row, 4).Value = report.PropertyAddress;
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 4).Style.Font.FontSize = 11;

        if (!string.IsNullOrEmpty(report.LenderName))
        {
            ws.Cell(row, 8).Value = $"LENDER: {report.LenderName}";
            ws.Cell(row, 8).Style.Font.FontSize = 9;
        }

        if (!string.IsNullOrEmpty(report.HoaName))
        {
            ws.Cell(row, 10).Value = $"HOA: {report.HoaName}";
            ws.Cell(row, 10).Style.Font.FontSize = 9;
        }

        row = 2;
        if (report.MonthlyMortgagePayment > 0)
        {
            ws.Cell(row, 8).Value = $"Monthly: {report.MonthlyMortgagePayment:C}";
            ws.Cell(row, 8).Style.Font.FontSize = 9;
        }
        if (report.HoaPaymentAmount > 0)
        {
            ws.Cell(row, 10).Value = $"{report.HoaFrequency}: {report.HoaPaymentAmount:C}";
            ws.Cell(row, 10).Style.Font.FontSize = 9;
        }

        // === ROW 4: Blank spacer ===
        row = 4;

        // === ROW 5: Empty (visual separator) ===
        row = 5;

        // === ROW 6: Column Headers ===
        row = 6;
        ws.Cell(row, 1).Value = "Income";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 11;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

        for (int m = 0; m < 12; m++)
        {
            ws.Cell(row, m + 2).Value = MonthHeaders[m];
            ws.Cell(row, m + 2).Style.Font.Bold = true;
            ws.Cell(row, m + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, m + 2).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
        }
        ws.Cell(row, 14).Value = "Total";
        ws.Cell(row, 14).Style.Font.Bold = true;
        ws.Cell(row, 14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 14).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

        // === ROW 7: Rent ===
        row = 7;
        WriteMonthlyRow(ws, row, "Rent", report.Rent);
        rowMap["Rent"] = row;

        if (report.PMTotalIncome > 0)
        {
            ws.Cell(row, 17).Value = report.PMTotalIncome;
            ws.Cell(row, 17).Style.NumberFormat.Format = "_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)";
            ws.Cell(row, 18).Value = "PM Total Income";
        }

        // === ROW 9: Pet Fees ===
        row = 9;
        if (report.PetFees.Any(v => v != 0))
            WriteMonthlyRow(ws, row, "Pet Fees", report.PetFees);

        // === ROW 10: Other Income ===
        row = 10;
        if (report.OtherIncome.Any(v => v != 0))
            WriteMonthlyRow(ws, row, "Other Income", report.OtherIncome);

        // === ROW 12: Total Income ===
        row = 12;
        rowMap["Total Income"] = row;
        ws.Cell(row, 1).Value = "Total Income";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.PaleGreen;
        for (int m = 0; m < 12; m++)
        {
            var colLetter = ws.Cell(row, m + 2).WorksheetColumn().ColumnLetter();
            ws.Cell(row, m + 2).FormulaA1 = $"=SUM({colLetter}7:{colLetter}10)";
            ws.Cell(row, m + 2).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, m + 2).Style.Fill.BackgroundColor = XLColor.PaleGreen;
        }
        ws.Cell(row, 14).FormulaA1 = "=SUM(B12:M12)";
        ws.Cell(row, 14).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 14).Style.Font.Bold = true;
        ws.Cell(row, 14).Style.Fill.BackgroundColor = XLColor.PaleGreen;

        // === ROW 13: Expenses Header ===
        row = 13;
        ws.Cell(row, 1).Value = "Expenses:";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 11;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightSalmon;
        for (int c = 2; c <= 14; c++)
            ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.LightSalmon;

        // === ROW 14: HVAC (from repair lines if exists) ===
        row = 14;
        var hvacLine = report.RepairLines.FirstOrDefault(r => r.SubCategory == "HVAC Repairs");
        if (hvacLine != null)
        {
            WriteMonthlyRow(ws, row, "HVAC", hvacLine.MonthlyAmounts);
            report.RepairLines.Remove(hvacLine);
        }

        // === ROW 15: Management Fee ===
        row = 15;
        WriteMonthlyRow(ws, row, "Management Fee", report.ManagementFee);
        rowMap["Management Fee"] = row;

        if (report.PMManagementFee > 0)
        {
            ws.Cell(row, 17).Value = report.PMManagementFee;
            ws.Cell(row, 17).Style.NumberFormat.Format = "_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)";
            ws.Cell(row, 18).Value = "PM Total Management Fee";
        }

        // === ROW 16: Other Professional Services ===
        row = 16;
        if (report.OtherProfessionalServices.Any(v => v != 0) || report.LeasingCommissions.Any(v => v != 0))
        {
            rowMap["Other Professional Services"] = row;
            decimal[] combined = new decimal[12];
            for (int i = 0; i < 12; i++)
                combined[i] = report.OtherProfessionalServices[i] + report.LeasingCommissions[i];
            WriteMonthlyRow(ws, row, "Other Professional Svc", combined);
        }

        // === ROW 17: Mortgage Interest ===
        row = 17;
        WriteMonthlyRow(ws, row, "Mortgage Interest", report.MortgageInterest);
        rowMap["Mortgage Interest"] = row;

        if (report.InterestPaid1098 > 0)
        {
            ws.Cell(row, 17).Value = report.InterestPaid1098;
            ws.Cell(row, 17).Style.NumberFormat.Format = "_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)";
            ws.Cell(row, 18).Value = "1098";
        }

        // === ROW 18: Property Taxes ===
        row = 18;
        WriteMonthlyRow(ws, row, "Property Taxes", report.PropertyTaxes);
        rowMap["Property Taxes"] = row;

        // === ROW 19: Insurance ===
        row = 19;
        WriteMonthlyRow(ws, row, "Insurance", report.InsurancePremium);
        rowMap["Insurance"] = row;

        // === ROW 20: HOA ===
        row = 20;
        WriteMonthlyRow(ws, row, "HOA", report.HoaDues);
        rowMap["HOA"] = row;

        // === ROW 21: Repairs header ===
        row = 21;
        ws.Cell(row, 1).Value = "Repairs";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.Italic = true;

        // === ROWS 22+: Dynamic repair lines ===
        row++;
        rowMap["RepairsStart"] = row;
        if (report.TenantChargeForRepair.Any(v => v != 0))
        {
            decimal[] negated = new decimal[12];
            for (int i = 0; i < 12; i++) negated[i] = -report.TenantChargeForRepair[i];
            WriteMonthlyRow(ws, row, "  Tenant Charge for Repair", negated);
            row++;
        }
        foreach (var repairLine in report.RepairLines.OrderBy(r => r.SubCategory))
        {
            WriteMonthlyRow(ws, row, $"  {repairLine.SubCategory}", repairLine.MonthlyAmounts);
            row++;
        }
        rowMap["RepairsEnd"] = Math.Max(rowMap["RepairsStart"], row - 1);

        // Capital Expenses
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
        rowMap["CapExEnd"] = Math.Max(rowMap["CapExStart"], row - 1);

        // Utilities
        rowMap["Utilities"] = row;
        if (report.Utilities.Any(v => v != 0))
        {
            WriteMonthlyRow(ws, row, "Utilities", report.Utilities);
            row++;
        }

        // === Total Expenses Row ===
        row++; // blank row
        int totalExpenseRow = row;
        rowMap["Total Expenses"] = row;
        ws.Cell(row, 1).Value = "Total Expenses";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightCoral;

        decimal[] monthlyExpenses = CalculateMonthlyExpenses(report);
        for (int m = 0; m < 12; m++)
        {
            var colLetter = ws.Cell(row, m + 2).WorksheetColumn().ColumnLetter();
            ws.Cell(row, m + 2).FormulaA1 = $"=SUM({colLetter}14:{colLetter}{row - 1})";
            ws.Cell(row, m + 2).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, m + 2).Style.Fill.BackgroundColor = XLColor.LightCoral;
        }
        ws.Cell(row, 14).FormulaA1 = $"=SUM(B{row}:M{row})";
        ws.Cell(row, 14).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 14).Style.Font.Bold = true;
        ws.Cell(row, 14).Style.Fill.BackgroundColor = XLColor.LightCoral;

        // === Net Income Row ===
        row += 2;
        int netIncomeRow = row;
        rowMap["Net Income"] = row;
        ws.Cell(row, 1).Value = "Net Income";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 12;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGoldenrodYellow;

        decimal[] monthlyIncome = new decimal[12];
        for (int m = 0; m < 12; m++)
        {
            monthlyIncome[m] = (report.Rent[m] + report.PetFees[m] + report.OtherIncome[m]) - monthlyExpenses[m];
            ws.Cell(row, m + 2).Value = monthlyIncome[m];
            ws.Cell(row, m + 2).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, m + 2).Style.Fill.BackgroundColor = XLColor.LightGoldenrodYellow;
        }
        ws.Cell(row, 14).Value = report.NetIncome;
        ws.Cell(row, 14).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 14).Style.Font.Bold = true;
        ws.Cell(row, 14).Style.Fill.BackgroundColor = XLColor.LightGoldenrodYellow;

        // === Below the line items ===
        row += 2;

        // Principal Payment
        rowMap["Mortgage Principal"] = row;
        ws.Cell(row, 1).Value = "Principal Payment";
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.DarkBlue;
        for (int m = 0; m < 12; m++)
        {
            ws.Cell(row, m + 2).Value = report.MortgagePrincipal[m];
            ws.Cell(row, m + 2).Style.NumberFormat.Format = "#,##0.00";
        }
        ws.Cell(row, 14).Value = report.MortgagePrincipal.Sum();
        ws.Cell(row, 14).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 14).Style.Font.Bold = true;
        row++;

        // Escrow
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
        ws.Cell(row, 14).Style.Font.Bold = true;
        row++;

        // Cash Flow = Net Income - Principal - Escrow
        ws.Cell(row, 1).Value = "Cash Flow";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.DarkGreen;
        for (int m = 0; m < 12; m++)
        {
            decimal cashFlow = monthlyIncome[m] - report.MortgagePrincipal[m] - report.EscrowPayments[m];
            ws.Cell(row, m + 2).Value = cashFlow;
            ws.Cell(row, m + 2).Style.NumberFormat.Format = "#,##0.00";
        }
        decimal totalCashFlow = report.NetIncome - report.MortgagePrincipal.Sum() - report.EscrowPayments.Sum();
        ws.Cell(row, 14).Value = totalCashFlow;
        ws.Cell(row, 14).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 14).Style.Font.Bold = true;
        row++;

        // Mortgage Payment
        ws.Cell(row, 1).Value = "Mortgage Payment";
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.DarkBlue;
        for (int m = 0; m < 12; m++)
        {
            ws.Cell(row, m + 2).Value = report.MortgagePayment[m];
            ws.Cell(row, m + 2).Style.NumberFormat.Format = "#,##0.00";
        }
        ws.Cell(row, 14).Value = report.MortgagePayment.Sum();
        ws.Cell(row, 14).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 14).Style.Font.Bold = true;
        row++;

        // NOI = Total Income - Operating Expenses (exclude mortgage interest for true NOI)
        decimal noi = report.TotalIncome - (report.ManagementFee.Sum() + report.LeasingCommissions.Sum()
            + report.OtherProfessionalServices.Sum() + report.PropertyTaxes.Sum()
            + report.InsurancePremium.Sum() + report.HoaDues.Sum()
            + report.RepairLines.Sum(r => r.MonthlyAmounts.Sum())
            + report.CapitalExpenses.Sum(c => c.MonthlyAmounts.Sum())
            + report.Utilities.Sum());

        ws.Cell(row, 1).Value = "NOI";
        rowMap["NOI"] = row;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 14).Value = noi;
        ws.Cell(row, 14).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 14).Style.Font.Bold = true;

        // === NOTES section ===
        row += 3;
        ws.Cell(row, 1).Value = "NOTES";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 11;
        for (int m = 0; m < 12; m++)
        {
            ws.Cell(row, m + 2).Value = MonthHeaders[m];
            ws.Cell(row, m + 2).Style.Font.Bold = true;
            ws.Cell(row, m + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Apply borders to data area
        var dataRange = ws.Range(6, 1, totalExpenseRow, 14);
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorderColor = XLColor.LightGray;
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

        // Print settings
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.FitToPages(1, 1);
    }

    private void WriteMonthlyRow(IXLWorksheet ws, int row, string label, decimal[] values)
    {
        ws.Cell(row, 1).Value = label;
        for (int m = 0; m < 12; m++)
        {
            if (values[m] != 0)
            {
                ws.Cell(row, m + 2).Value = values[m];
                ws.Cell(row, m + 2).Style.NumberFormat.Format = "#,##0.00";
            }
        }
        decimal total = values.Sum();
        if (total != 0)
        {
            ws.Cell(row, 14).FormulaA1 = $"=SUM(B{row}:M{row})";
            ws.Cell(row, 14).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 14).Style.Font.Bold = true;
        }
    }

    private decimal[] CalculateMonthlyExpenses(PropertyReportDto report)
    {
        decimal[] expenses = new decimal[12];
        for (int m = 0; m < 12; m++)
        {
            expenses[m] = report.ManagementFee[m]
                + report.LeasingCommissions[m]
                + report.OtherProfessionalServices[m]
                + report.MortgageInterest[m]
                + report.PropertyTaxes[m]
                + report.InsurancePremium[m]
                + report.HoaDues[m]
                + report.RepairLines.Sum(r => r.MonthlyAmounts[m])
                - report.TenantChargeForRepair[m]
                + report.CapitalExpenses.Sum(c => c.MonthlyAmounts[m])
                + report.Utilities[m];
        }
        return expenses;
    }

    private static string StreetNameWithoutNumber(string street)
    {
        // Strip leading digits and whitespace to sort by the street name portion
        return Regex.Replace(street, @"^\d+\s*", "");
    }
}
