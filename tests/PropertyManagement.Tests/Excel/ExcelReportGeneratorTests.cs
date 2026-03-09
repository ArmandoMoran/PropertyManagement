using ClosedXML.Excel;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Infrastructure.Excel;

namespace PropertyManagement.Tests.Excel;

public class ExcelReportGeneratorTests
{
    private readonly ExcelReportGenerator _sut = new();

    private PropertyReportDto CreateTestReport(string sheetName = "Test Property")
    {
        return new PropertyReportDto
        {
            PropertyId = 1,
            PropertyAddress = "123 Test St, San Antonio, TX 78000",
            SheetName = sheetName,
            LenderName = "Test Bank",
            MonthlyMortgagePayment = 1500,
            HoaName = "Test HOA",
            HoaPaymentAmount = 75,
            HoaFrequency = "Quarterly",
            Year = 2025
        };
    }

    [Fact]
    public async Task GenerateWorkbook_ReturnsNonEmptyByteArray()
    {
        var reports = new List<PropertyReportDto> { CreateTestReport() };

        var result = await _sut.GenerateWorkbookAsync(reports, 2025);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task GenerateWorkbook_CreatesOneSheetPerProperty()
    {
        var reports = new List<PropertyReportDto>
        {
            CreateTestReport("Property A"),
            CreateTestReport("Property B"),
            CreateTestReport("Property C"),
        };

        var bytes = await _sut.GenerateWorkbookAsync(reports, 2025);
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);

        Assert.Equal(3, wb.Worksheets.Count(w => w.Name != "SUMMARY"));
        Assert.Contains(wb.Worksheets, ws => ws.Name == "Property A");
        Assert.Contains(wb.Worksheets, ws => ws.Name == "Property B");
        Assert.Contains(wb.Worksheets, ws => ws.Name == "Property C");
    }

    [Fact]
    public async Task GenerateWorkbook_SheetName_TruncatedTo31Chars()
    {
        var longName = new string('A', 50); // 50 chars
        var reports = new List<PropertyReportDto> { CreateTestReport(longName) };

        var bytes = await _sut.GenerateWorkbookAsync(reports, 2025);
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);

        Assert.Equal(31, wb.Worksheets.First(w => w.Name != "SUMMARY").Name.Length);
    }

    [Fact]
    public async Task GenerateWorkbook_HeaderRow_ContainsTitle()
    {
        var reports = new List<PropertyReportDto> { CreateTestReport() };

        var bytes = await _sut.GenerateWorkbookAsync(reports, 2025);
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First(w => w.Name != "SUMMARY");

        Assert.Equal("Rental Profit & Loss - 2025", ws.Cell(1, 1).GetString());
    }

    [Fact]
    public async Task GenerateWorkbook_Row6_ContainsMonthHeaders()
    {
        var reports = new List<PropertyReportDto> { CreateTestReport() };

        var bytes = await _sut.GenerateWorkbookAsync(reports, 2025);
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First(w => w.Name != "SUMMARY");

        Assert.Equal("Income", ws.Cell(6, 1).GetString());
        Assert.Equal("Jan", ws.Cell(6, 2).GetString());
        Assert.Equal("Dec", ws.Cell(6, 13).GetString());
        Assert.Equal("Total", ws.Cell(6, 14).GetString());
    }

    [Fact]
    public async Task GenerateWorkbook_Row7_ContainsRent()
    {
        var report = CreateTestReport();
        report.Rent[0] = 1500; // Jan
        report.Rent[1] = 1500; // Feb
        var reports = new List<PropertyReportDto> { report };

        var bytes = await _sut.GenerateWorkbookAsync(reports, 2025);
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First(w => w.Name != "SUMMARY");

        Assert.Equal("Rent", ws.Cell(7, 1).GetString());
        Assert.Equal(1500, ws.Cell(7, 2).GetDouble());
        Assert.Equal(1500, ws.Cell(7, 3).GetDouble());
        Assert.Equal(3000, ws.Cell(7, 14).GetDouble()); // Total
    }

    [Fact]
    public async Task GenerateWorkbook_Row12_TotalIncome_SumsAllIncome()
    {
        var report = CreateTestReport();
        report.Rent[0] = 1500;
        report.PetFees[0] = 50;
        var reports = new List<PropertyReportDto> { report };

        var bytes = await _sut.GenerateWorkbookAsync(reports, 2025);
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First(w => w.Name != "SUMMARY");

        Assert.Equal("Total Income", ws.Cell(12, 1).GetString());
        Assert.Equal(1550, ws.Cell(12, 2).GetDouble()); // Jan: 1500 + 50
    }

    [Fact]
    public async Task GenerateWorkbook_Row14_HvacExtractedFromRepairLines()
    {
        var report = CreateTestReport();
        report.RepairLines.Add(new RepairLineDto
        {
            SubCategory = "HVAC Repairs",
            MonthlyAmounts = new decimal[12]
        });
        report.RepairLines[0].MonthlyAmounts[3] = 350; // Apr
        report.RepairLines.Add(new RepairLineDto
        {
            SubCategory = "Plumbing Repairs",
            MonthlyAmounts = new decimal[12]
        });
        report.RepairLines[1].MonthlyAmounts[5] = 200; // Jun
        var reports = new List<PropertyReportDto> { report };

        var bytes = await _sut.GenerateWorkbookAsync(reports, 2025);
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First(w => w.Name != "SUMMARY");

        // Row 14 should have HVAC
        Assert.Equal("HVAC", ws.Cell(14, 1).GetString());
        Assert.Equal(350, ws.Cell(14, 5).GetDouble()); // Apr (col 5 = month index 3 + 2)

        // Plumbing should appear in the dynamic repairs section at row 22
        Assert.Equal("  Plumbing Repairs", ws.Cell(22, 1).GetString());
        Assert.Equal(200, ws.Cell(22, 7).GetDouble()); // Jun (col 7 = month index 5 + 2)
    }

    [Fact]
    public async Task GenerateWorkbook_Row15_ManagementFee()
    {
        var report = CreateTestReport();
        report.ManagementFee[0] = 142.80m;
        var reports = new List<PropertyReportDto> { report };

        var bytes = await _sut.GenerateWorkbookAsync(reports, 2025);
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First(w => w.Name != "SUMMARY");

        Assert.Equal("Management Fee", ws.Cell(15, 1).GetString());
        Assert.Equal(142.80, ws.Cell(15, 2).GetDouble());
    }

    [Fact]
    public async Task GenerateWorkbook_ContainsLenderInfo()
    {
        var report = CreateTestReport();
        report.LenderName = "Chase Bank";
        report.MonthlyMortgagePayment = 1500;
        var reports = new List<PropertyReportDto> { report };

        var bytes = await _sut.GenerateWorkbookAsync(reports, 2025);
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First(w => w.Name != "SUMMARY");

        Assert.Equal("LENDER: Chase Bank", ws.Cell(1, 8).GetString());
        Assert.Equal("Monthly: $1,500.00", ws.Cell(2, 8).GetString());
    }

    [Fact]
    public async Task GenerateWorkbook_ContainsHoaInfo()
    {
        var report = CreateTestReport();
        report.HoaName = "Sunrise HOA";
        report.HoaPaymentAmount = 75;
        report.HoaFrequency = "Quarterly";
        var reports = new List<PropertyReportDto> { report };

        var bytes = await _sut.GenerateWorkbookAsync(reports, 2025);
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First(w => w.Name != "SUMMARY");

        Assert.Equal("HOA: Sunrise HOA", ws.Cell(1, 10).GetString());
        Assert.Equal("Quarterly: $75.00", ws.Cell(2, 10).GetString());
    }

    [Fact]
    public async Task GenerateWorkbook_ExpenseTotals_IncludeAllCategories()
    {
        var report = CreateTestReport();
        report.ManagementFee[0] = 100;
        report.MortgageInterest[0] = 200;
        report.PropertyTaxes[0] = 300;
        report.InsurancePremium[0] = 400;
        report.HoaDues[0] = 50;
        report.Utilities[0] = 75;
        report.RepairLines.Add(new RepairLineDto
        {
            SubCategory = "Plumbing Repairs",
            MonthlyAmounts = new decimal[12]
        });
        report.RepairLines[0].MonthlyAmounts[0] = 150;
        var reports = new List<PropertyReportDto> { report };

        var bytes = await _sut.GenerateWorkbookAsync(reports, 2025);
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First(w => w.Name != "SUMMARY");

        // Find the Total Expenses row
        int totalExpenseRow = -1;
        for (int r = 1; r <= 50; r++)
        {
            if (ws.Cell(r, 1).GetString() == "Total Expenses")
            {
                totalExpenseRow = r;
                break;
            }
        }

        Assert.NotEqual(-1, totalExpenseRow);
        decimal expectedTotal = 100 + 200 + 300 + 400 + 50 + 75 + 150;
        Assert.Equal((double)expectedTotal, ws.Cell(totalExpenseRow, 14).GetDouble());
    }

    [Fact]
    public async Task GenerateWorkbook_NoiCalculation_ExcludesMortgageInterest()
    {
        var report = CreateTestReport();
        report.Rent[0] = 2000;
        report.ManagementFee[0] = 100;
        report.MortgageInterest[0] = 500; // Should be excluded from NOI
        report.PropertyTaxes[0] = 200;
        var reports = new List<PropertyReportDto> { report };

        var bytes = await _sut.GenerateWorkbookAsync(reports, 2025);
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First(w => w.Name != "SUMMARY");

        // Find the NOI row
        int noiRow = -1;
        for (int r = 1; r <= 50; r++)
        {
            if (ws.Cell(r, 1).GetString() == "NOI")
            {
                noiRow = r;
                break;
            }
        }

        Assert.NotEqual(-1, noiRow);
        // NOI = TotalIncome - (MgmtFee + PropertyTaxes) = 2000 - (100 + 200) = 1700
        // NOI excludes mortgage interest
        decimal expectedNoi = 2000 - (100 + 200);
        Assert.Equal((double)expectedNoi, ws.Cell(noiRow, 14).GetDouble());
    }

    [Fact]
    public async Task GenerateWorkbook_EmptyReport_GeneratesWithoutError()
    {
        var report = CreateTestReport();
        var reports = new List<PropertyReportDto> { report };

        var bytes = await _sut.GenerateWorkbookAsync(reports, 2025);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public async Task GenerateWorkbook_ZeroValues_NotWrittenToCells()
    {
        var report = CreateTestReport();
        // All arrays are initialized to zero
        var reports = new List<PropertyReportDto> { report };

        var bytes = await _sut.GenerateWorkbookAsync(reports, 2025);
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First(w => w.Name != "SUMMARY");

        // Row 7 (Rent) - zero cells should be empty
        Assert.True(ws.Cell(7, 2).IsEmpty());
    }

    [Fact]
    public async Task GenerateWorkbook_NetIncomeRow_CalculatesCorrectly()
    {
        var report = CreateTestReport();
        report.Rent[0] = 2000;
        report.ManagementFee[0] = 160;
        report.MortgageInterest[0] = 300;
        var reports = new List<PropertyReportDto> { report };

        var bytes = await _sut.GenerateWorkbookAsync(reports, 2025);
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First(w => w.Name != "SUMMARY");

        // Find the Net Income row
        int netIncomeRow = -1;
        for (int r = 1; r <= 50; r++)
        {
            if (ws.Cell(r, 1).GetString() == "Net Income")
            {
                netIncomeRow = r;
                break;
            }
        }

        Assert.NotEqual(-1, netIncomeRow);
        // Net Income Jan = 2000 - (160 + 300) = 1540
        Assert.Equal(1540, ws.Cell(netIncomeRow, 2).GetDouble());
    }

    [Fact]
    public async Task GenerateWorkbook_PrincipalAndEscrow_BelowTheLine()
    {
        var report = CreateTestReport();
        report.MortgagePrincipal[0] = 500;
        report.EscrowPayments[0] = 200;
        var reports = new List<PropertyReportDto> { report };

        var bytes = await _sut.GenerateWorkbookAsync(reports, 2025);
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First(w => w.Name != "SUMMARY");

        // Find Principal Payment row
        int principalRow = -1;
        int escrowRow = -1;
        for (int r = 1; r <= 50; r++)
        {
            var val = ws.Cell(r, 1).GetString();
            if (val == "Principal Payment") principalRow = r;
            if (val == "Escrow") escrowRow = r;
        }

        Assert.NotEqual(-1, principalRow);
        Assert.NotEqual(-1, escrowRow);
        Assert.Equal(500, ws.Cell(principalRow, 2).GetDouble());
        Assert.Equal(200, ws.Cell(escrowRow, 2).GetDouble());
    }

    [Fact]
    public async Task GenerateWorkbook_DynamicRepairLines_Sorted()
    {
        var report = CreateTestReport();
        report.RepairLines.Add(new RepairLineDto { SubCategory = "Plumbing Repairs", MonthlyAmounts = new decimal[12] });
        report.RepairLines.Add(new RepairLineDto { SubCategory = "Electrical Repairs", MonthlyAmounts = new decimal[12] });
        report.RepairLines.Add(new RepairLineDto { SubCategory = "Appliance Repairs", MonthlyAmounts = new decimal[12] });
        report.RepairLines[0].MonthlyAmounts[0] = 100;
        report.RepairLines[1].MonthlyAmounts[0] = 200;
        report.RepairLines[2].MonthlyAmounts[0] = 300;
        var reports = new List<PropertyReportDto> { report };

        var bytes = await _sut.GenerateWorkbookAsync(reports, 2025);
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First(w => w.Name != "SUMMARY");

        // Repair lines start at row 22, should be sorted alphabetically
        Assert.Equal("  Appliance Repairs", ws.Cell(22, 1).GetString());
        Assert.Equal("  Electrical Repairs", ws.Cell(23, 1).GetString());
        Assert.Equal("  Plumbing Repairs", ws.Cell(24, 1).GetString());
    }

    [Fact]
    public async Task GenerateWorkbook_CapitalExpenses_IncludedInReport()
    {
        var report = CreateTestReport();
        report.CapitalExpenses.Add(new CapitalExpenseLineDto
        {
            SubCategory = "New Appliances",
            MonthlyAmounts = new decimal[12]
        });
        report.CapitalExpenses[0].MonthlyAmounts[6] = 1200; // Jul
        var reports = new List<PropertyReportDto> { report };

        var bytes = await _sut.GenerateWorkbookAsync(reports, 2025);
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First(w => w.Name != "SUMMARY");

        // Find Capital Expenses header
        int capExpRow = -1;
        for (int r = 1; r <= 50; r++)
        {
            if (ws.Cell(r, 1).GetString() == "Capital Expenses")
            {
                capExpRow = r;
                break;
            }
        }

        Assert.NotEqual(-1, capExpRow);
        // Next row should have the line item
        Assert.Equal("  New Appliances", ws.Cell(capExpRow + 1, 1).GetString());
        Assert.Equal(1200, ws.Cell(capExpRow + 1, 8).GetDouble()); // Jul (col 8 = index 6 + 2)
    }
}
