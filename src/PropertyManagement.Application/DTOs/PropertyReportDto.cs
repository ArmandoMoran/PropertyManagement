namespace PropertyManagement.Application.DTOs;

/// <summary>
/// Represents the complete profit & loss report data for a single property for a given year.
/// Maps to the Excel template layout.
/// </summary>
public class PropertyReportDto
{
    public int PropertyId { get; set; }
    public string PropertyAddress { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty;
    public string? LenderName { get; set; }
    public decimal MonthlyMortgagePayment { get; set; }
    public string? HoaName { get; set; }
    public decimal HoaPaymentAmount { get; set; }
    public string? HoaFrequency { get; set; }
    public decimal InterestPaid1098 { get; set; }
    public decimal PMTotalIncome { get; set; }
    public decimal PMManagementFee { get; set; }
    public int Year { get; set; }

    // Monthly data arrays (index 0 = Jan, 11 = Dec)
    public decimal[] Rent { get; set; } = new decimal[12];
    public decimal[] TenantChargeForRepair { get; set; } = new decimal[12];
    public decimal[] PetFees { get; set; } = new decimal[12];
    public decimal[] OtherIncome { get; set; } = new decimal[12];

    // Expense rows
    public decimal[] ManagementFee { get; set; } = new decimal[12];
    public decimal[] LeasingCommissions { get; set; } = new decimal[12];
    public decimal[] OtherProfessionalServices { get; set; } = new decimal[12];
    public decimal[] MortgageInterest { get; set; } = new decimal[12];
    public decimal[] PropertyTaxes { get; set; } = new decimal[12];
    public decimal[] InsurancePremium { get; set; } = new decimal[12];
    public decimal[] HoaDues { get; set; } = new decimal[12];

    // Dynamic repair rows (SubCategory -> monthly amounts)
    public List<RepairLineDto> RepairLines { get; set; } = new();

    // Below-the-line items
    public decimal[] MortgagePrincipal { get; set; } = new decimal[12];
    public decimal[] EscrowPayments { get; set; } = new decimal[12];
    public decimal[] MortgagePayment { get; set; } = new decimal[12];

    // Capital Expenses
    public List<CapitalExpenseLineDto> CapitalExpenses { get; set; } = new();

    // Utility rows
    public decimal[] Utilities { get; set; } = new decimal[12];

    // Calculated totals
    public decimal TotalIncome => Rent.Sum() + PetFees.Sum() + OtherIncome.Sum();
    public decimal TotalExpenses => ManagementFee.Sum() + LeasingCommissions.Sum()
        + OtherProfessionalServices.Sum() + MortgageInterest.Sum()
        + PropertyTaxes.Sum() + InsurancePremium.Sum() + HoaDues.Sum()
        + RepairLines.Sum(r => r.MonthlyAmounts.Sum())
        - TenantChargeForRepair.Sum()
        + CapitalExpenses.Sum(c => c.MonthlyAmounts.Sum())
        + Utilities.Sum();
    public decimal NetIncome => TotalIncome - TotalExpenses;
}

public class RepairLineDto
{
    public string SubCategory { get; set; } = string.Empty;
    public decimal[] MonthlyAmounts { get; set; } = new decimal[12];
    public decimal Total => MonthlyAmounts.Sum();
}

public class CapitalExpenseLineDto
{
    public string SubCategory { get; set; } = string.Empty;
    public decimal[] MonthlyAmounts { get; set; } = new decimal[12];
    public decimal Total => MonthlyAmounts.Sum();
}
