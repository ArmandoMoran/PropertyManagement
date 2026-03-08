using PropertyManagement.Application.DTOs;

namespace PropertyManagement.Tests.DTOs;

public class PropertyReportDtoTests
{
    [Fact]
    public void TotalIncome_SumsAllIncomeArrays()
    {
        var dto = new PropertyReportDto();
        dto.Rent[0] = 1500;
        dto.Rent[1] = 1500;
        dto.TenantChargeForRepair[0] = 30;
        dto.PetFees[0] = 50;
        dto.OtherIncome[2] = 100;

        Assert.Equal(3180m, dto.TotalIncome);
    }

    [Fact]
    public void TotalExpenses_SumsAllExpenseCategories()
    {
        var dto = new PropertyReportDto();
        dto.ManagementFee[0] = 100;
        dto.LeasingCommissions[0] = 200;
        dto.OtherProfessionalServices[0] = 50;
        dto.MortgageInterest[0] = 300;
        dto.PropertyTaxes[0] = 400;
        dto.InsurancePremium[0] = 500;
        dto.HoaDues[0] = 75;
        dto.Utilities[0] = 85;
        dto.RepairLines.Add(new RepairLineDto { SubCategory = "Plumbing", MonthlyAmounts = new decimal[12] });
        dto.RepairLines[0].MonthlyAmounts[0] = 150;
        dto.CapitalExpenses.Add(new CapitalExpenseLineDto { SubCategory = "Appliances", MonthlyAmounts = new decimal[12] });
        dto.CapitalExpenses[0].MonthlyAmounts[0] = 250;

        decimal expected = 100 + 200 + 50 + 300 + 400 + 500 + 75 + 85 + 150 + 250;
        Assert.Equal(expected, dto.TotalExpenses);
    }

    [Fact]
    public void NetIncome_IsIncomeMinusExpenses()
    {
        var dto = new PropertyReportDto();
        dto.Rent[0] = 2000;
        dto.ManagementFee[0] = 200;
        dto.MortgageInterest[0] = 300;

        Assert.Equal(1500m, dto.NetIncome); // 2000 - (200 + 300)
    }

    [Fact]
    public void NewDto_AllArraysInitializedToZero()
    {
        var dto = new PropertyReportDto();

        Assert.Equal(12, dto.Rent.Length);
        Assert.All(dto.Rent, v => Assert.Equal(0, v));
        Assert.All(dto.ManagementFee, v => Assert.Equal(0, v));
        Assert.All(dto.MortgageInterest, v => Assert.Equal(0, v));
        Assert.All(dto.PropertyTaxes, v => Assert.Equal(0, v));
        Assert.All(dto.InsurancePremium, v => Assert.Equal(0, v));
        Assert.All(dto.HoaDues, v => Assert.Equal(0, v));
        Assert.All(dto.MortgagePrincipal, v => Assert.Equal(0, v));
        Assert.All(dto.EscrowPayments, v => Assert.Equal(0, v));
        Assert.All(dto.MortgagePayment, v => Assert.Equal(0, v));
        Assert.All(dto.Utilities, v => Assert.Equal(0, v));
    }

    [Fact]
    public void RepairLineDto_Total_SumsAllMonths()
    {
        var line = new RepairLineDto
        {
            SubCategory = "Plumbing Repairs",
            MonthlyAmounts = new decimal[12]
        };
        line.MonthlyAmounts[0] = 100;
        line.MonthlyAmounts[5] = 200;
        line.MonthlyAmounts[11] = 150;

        Assert.Equal(450m, line.Total);
    }

    [Fact]
    public void CapitalExpenseLineDto_Total_SumsAllMonths()
    {
        var line = new CapitalExpenseLineDto
        {
            SubCategory = "New Appliances",
            MonthlyAmounts = new decimal[12]
        };
        line.MonthlyAmounts[6] = 1200;

        Assert.Equal(1200m, line.Total);
    }

    [Fact]
    public void TotalExpenses_NoRepairsOrCapEx_StillCalculatesCorrectly()
    {
        var dto = new PropertyReportDto();
        dto.ManagementFee[0] = 100;
        dto.MortgageInterest[0] = 200;

        // No repair lines or capital expenses
        Assert.Equal(300m, dto.TotalExpenses);
    }

    [Fact]
    public void TotalExpenses_MultipleRepairLines_AllIncluded()
    {
        var dto = new PropertyReportDto();
        dto.RepairLines.Add(new RepairLineDto { SubCategory = "Plumbing", MonthlyAmounts = new decimal[12] });
        dto.RepairLines.Add(new RepairLineDto { SubCategory = "HVAC", MonthlyAmounts = new decimal[12] });
        dto.RepairLines.Add(new RepairLineDto { SubCategory = "Electrical", MonthlyAmounts = new decimal[12] });
        dto.RepairLines[0].MonthlyAmounts[0] = 100;
        dto.RepairLines[1].MonthlyAmounts[1] = 200;
        dto.RepairLines[2].MonthlyAmounts[2] = 300;

        Assert.Equal(600m, dto.TotalExpenses);
    }
}
