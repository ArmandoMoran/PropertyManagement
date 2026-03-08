using Moq;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Services;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Interfaces;

namespace PropertyManagement.Tests.Services;

public class ReportServiceTests
{
    private readonly Mock<IPropertyRepository> _propertyRepoMock;
    private readonly Mock<ITransactionRepository> _transactionRepoMock;
    private readonly Mock<ILenderRepository> _lenderRepoMock;
    private readonly Mock<IHoaRepository> _hoaRepoMock;
    private readonly Mock<IInsuranceRepository> _insuranceRepoMock;
    private readonly ReportService _sut;

    public ReportServiceTests()
    {
        _propertyRepoMock = new Mock<IPropertyRepository>();
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _lenderRepoMock = new Mock<ILenderRepository>();
        _hoaRepoMock = new Mock<IHoaRepository>();
        _insuranceRepoMock = new Mock<IInsuranceRepository>();

        _sut = new ReportService(
            _propertyRepoMock.Object,
            _transactionRepoMock.Object,
            _lenderRepoMock.Object,
            _hoaRepoMock.Object,
            _insuranceRepoMock.Object);
    }

    private Property CreateTestProperty(int id = 1, string street = "123 Test St")
    {
        return new Property
        {
            PropertyId = id,
            FullAddress = $"{street}, San Antonio, TX 78000",
            Street = street,
            City = "San Antonio",
            State = "TX",
            ZipCode = "78000"
        };
    }

    private Transaction CreateTransaction(
        DateTime date,
        string? category,
        string? subCategory,
        decimal amount,
        int propertyId = 1,
        string name = "Test Vendor")
    {
        return new Transaction
        {
            TransactionId = 0,
            TransactionDate = date,
            Category = category,
            SubCategory = subCategory,
            Amount = amount,
            PropertyId = propertyId,
            Name = name
        };
    }

    private void SetupMocks(Property property, List<Transaction> transactions,
        Lender? lender = null, HoaInfo? hoa = null, InsurancePremium? premium = null, Insurance? insurance = null)
    {
        _propertyRepoMock.Setup(r => r.GetAllPropertiesAsync())
            .ReturnsAsync(new[] { property });
        _transactionRepoMock.Setup(r => r.GetTransactionsByPropertyAndYearAsync(property.PropertyId, It.IsAny<int>()))
            .ReturnsAsync(transactions);
        _lenderRepoMock.Setup(r => r.GetLenderByPropertyIdAsync(property.PropertyId))
            .ReturnsAsync(lender);
        _hoaRepoMock.Setup(r => r.GetHoaByPropertyIdAsync(property.PropertyId))
            .ReturnsAsync(hoa);
        _insuranceRepoMock.Setup(r => r.GetPremiumByPropertyAndYearAsync(property.PropertyId, It.IsAny<int>()))
            .ReturnsAsync(premium);
        _insuranceRepoMock.Setup(r => r.GetInsuranceByPropertyIdAsync(property.PropertyId))
            .ReturnsAsync(insurance);
    }

    // ===== INCOME TESTS =====

    [Fact]
    public async Task RentTransactions_MappedToRentArray()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 1, 15), "Income", "Rents", 1500),
            CreateTransaction(new DateTime(2025, 2, 15), "Income", "Rents", 1500),
            CreateTransaction(new DateTime(2025, 3, 15), "Income", "Section 8 Rents", 800),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Single(reports);
        Assert.Equal(1500, reports[0].Rent[0]); // Jan
        Assert.Equal(1500, reports[0].Rent[1]); // Feb
        Assert.Equal(800, reports[0].Rent[2]);  // Mar (Section 8)
    }

    [Fact]
    public async Task PetFeeTransactions_MappedToPetFeesArray()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 5, 1), "Income", "Pet Fees", 50),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(50, reports[0].PetFees[4]); // May
    }

    [Fact]
    public async Task OtherIncomeTransactions_MappedToOtherIncomeArray()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 7, 1), "Income", null, 100),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(100, reports[0].OtherIncome[6]); // Jul
    }

    [Fact]
    public async Task MultipleRentsInSameMonth_AreAccumulated()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 2, 1), "Income", "Rents", 1500),
            CreateTransaction(new DateTime(2025, 2, 15), "Income", "Rents", 200),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(1700, reports[0].Rent[1]); // Feb accumulated
    }

    // ===== MANAGEMENT FEES TESTS =====

    [Fact]
    public async Task ManagementFeeTransactions_MappedToManagementFeeArray()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 1, 31), "Management Fees", "Property Management", -142.80m),
            CreateTransaction(new DateTime(2025, 2, 28), "Management Fees", "Property Management", -142.80m),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(142.80m, reports[0].ManagementFee[0]); // Jan (abs of -142.80)
        Assert.Equal(142.80m, reports[0].ManagementFee[1]); // Feb
    }

    [Fact]
    public async Task LeasingCommissions_MappedToLeasingCommissionsArray()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 6, 1), "Management Fees", "Leasing Commissions", -500),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(500, reports[0].LeasingCommissions[5]); // Jun
    }

    [Fact]
    public async Task ManagementFeesNullSubCategory_MappedToManagementFee()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 3, 1), "Management Fees", null, -100),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(100, reports[0].ManagementFee[2]); // Mar
    }

    // ===== MORTGAGE & LOANS TESTS =====

    [Fact]
    public async Task MortgageInterest_MappedCorrectly()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 1, 1), "Mortgages & Loans", "Mortgage Interest", -133.65m),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(133.65m, reports[0].MortgageInterest[0]); // Jan
    }

    [Fact]
    public async Task MortgagePrincipal_MappedCorrectly()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 1, 1), "Mortgages & Loans", "Mortgage Principal", -619.96m),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(619.96m, reports[0].MortgagePrincipal[0]); // Jan
    }

    [Fact]
    public async Task MortgagePayment_MappedCorrectly()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 1, 1), "Mortgages & Loans", "Mortgage Payment", -1380.12m),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(1380.12m, reports[0].MortgagePayment[0]); // Jan
    }

    [Fact]
    public async Task OtherLoanPayment_TreatedAsMortgagePayment()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 4, 1), "Mortgages & Loans", "Other Loan Payment", -500),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(500, reports[0].MortgagePayment[3]); // Apr
    }

    // ===== TAXES TESTS =====

    [Fact]
    public async Task PropertyTaxPayment_NegativeAmount_MappedAsPositiveExpense()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 12, 1), "Taxes", "Property Taxes", -5633.82m),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(5633.82m, reports[0].PropertyTaxes[11]); // Dec
    }

    [Fact]
    public async Task PropertyTaxRefund_PositiveAmount_KeptAsPositive()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 12, 1), "Taxes", "Property Taxes", 5633.82m),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        // Positive taxes (escrow disbursement) are stored as positive
        Assert.Equal(5633.82m, reports[0].PropertyTaxes[11]);
    }

    // ===== INSURANCE TESTS =====

    [Fact]
    public async Task InsuranceTransaction_MappedToInsurancePremiumArray()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 12, 1), "Insurance", "Rental Dwelling", 1693m),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(1693m, reports[0].InsurancePremium[11]); // Dec
    }

    [Fact]
    public async Task InsuranceNullSubCategory_MappedToInsurancePremiumArray()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 6, 1), "Insurance", null, -800),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(800, reports[0].InsurancePremium[5]); // Jun
    }

    [Fact]
    public async Task InsuranceFallback_UsesInsurancePremiumHistory_WhenNoTransactions()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>(); // No insurance transactions
        var premium = new InsurancePremium
        {
            PremiumId = 1,
            InsuranceId = 1,
            PolicyYear = 2025,
            AnnualPremium = 1500m
        };
        var insurance = new Insurance
        {
            InsuranceId = 1,
            PropertyId = 1,
            RenewalDate = new DateTime(2025, 8, 1) // August renewal
        };
        SetupMocks(property, transactions, premium: premium, insurance: insurance);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(1500m, reports[0].InsurancePremium[7]); // Aug (renewal month)
        Assert.Equal(0, reports[0].InsurancePremium[0]); // Other months should be 0
    }

    [Fact]
    public async Task InsuranceFallback_PlacesInJanuary_WhenNoRenewalDate()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>();
        var premium = new InsurancePremium
        {
            PremiumId = 1,
            InsuranceId = 1,
            PolicyYear = 2025,
            AnnualPremium = 1200m
        };
        var insurance = new Insurance
        {
            InsuranceId = 1,
            PropertyId = 1,
            RenewalDate = null
        };
        SetupMocks(property, transactions, premium: premium, insurance: insurance);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(1200m, reports[0].InsurancePremium[0]); // Jan fallback
    }

    [Fact]
    public async Task InsuranceFallback_NotUsed_WhenTransactionsExist()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 12, 1), "Insurance", "Rental Dwelling", 1693m),
        };
        var premium = new InsurancePremium
        {
            PremiumId = 1,
            InsuranceId = 1,
            PolicyYear = 2025,
            AnnualPremium = 9999m // Should NOT be used
        };
        SetupMocks(property, transactions, premium: premium);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(1693m, reports[0].InsurancePremium[11]);
        // Ensure the fallback value was NOT applied
        Assert.Equal(1693m, reports[0].InsurancePremium.Sum());
    }

    // ===== HOA TESTS =====

    [Fact]
    public async Task HoaDues_MappedCorrectly()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 3, 1), "Admin & Other", "HOA Dues", -69.58m),
            CreateTransaction(new DateTime(2025, 6, 1), "Admin & Other", "HOA Dues", -69.58m),
            CreateTransaction(new DateTime(2025, 9, 1), "Admin & Other", "HOA Dues", -69.58m),
            CreateTransaction(new DateTime(2025, 12, 1), "Admin & Other", "HOA Dues", -76.53m),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(69.58m, reports[0].HoaDues[2]);  // Mar
        Assert.Equal(69.58m, reports[0].HoaDues[5]);  // Jun
        Assert.Equal(69.58m, reports[0].HoaDues[8]);  // Sep
        Assert.Equal(76.53m, reports[0].HoaDues[11]); // Dec
    }

    // ===== REPAIRS & MAINTENANCE TESTS =====

    [Fact]
    public async Task RepairTransaction_NegativeAmount_MappedToRepairLines()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 1, 15), "Repairs & Maintenance", "Plumbing Repairs", -156.20m),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Single(reports[0].RepairLines);
        Assert.Equal("Plumbing Repairs", reports[0].RepairLines[0].SubCategory);
        Assert.Equal(156.20m, reports[0].RepairLines[0].MonthlyAmounts[0]); // Jan
    }

    [Fact]
    public async Task RepairTransaction_NullSubCategory_MappedAsGeneralRepairs()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 10, 17), "Repairs & Maintenance", null, -135.00m),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Single(reports[0].RepairLines);
        Assert.Equal("General Repairs", reports[0].RepairLines[0].SubCategory);
        Assert.Equal(135.00m, reports[0].RepairLines[0].MonthlyAmounts[9]); // Oct
    }

    [Fact]
    public async Task RepairTransaction_MultipleSubCategories_GroupedSeparately()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 1, 15), "Repairs & Maintenance", "Plumbing Repairs", -156.20m),
            CreateTransaction(new DateTime(2025, 6, 5), "Repairs & Maintenance", "Roof Repairs", -325.00m),
            CreateTransaction(new DateTime(2025, 10, 17), "Repairs & Maintenance", null, -135.00m),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(3, reports[0].RepairLines.Count);
        Assert.Contains(reports[0].RepairLines, r => r.SubCategory == "Plumbing Repairs");
        Assert.Contains(reports[0].RepairLines, r => r.SubCategory == "Roof Repairs");
        Assert.Contains(reports[0].RepairLines, r => r.SubCategory == "General Repairs");
    }

    [Fact]
    public async Task RepairTransaction_SameSubCategorySameMonth_Accumulated()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 6, 10), "Repairs & Maintenance", "Plumbing Repairs", -150.00m),
            CreateTransaction(new DateTime(2025, 6, 27), "Repairs & Maintenance", "Plumbing Repairs", -630.00m),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Single(reports[0].RepairLines);
        Assert.Equal(780.00m, reports[0].RepairLines[0].MonthlyAmounts[5]); // Jun accumulated
    }

    [Theory]
    [InlineData("HVAC Repairs")]
    [InlineData("Plumbing Repairs")]
    [InlineData("Roof Repairs")]
    [InlineData("Electrical Repairs")]
    [InlineData("Appliance Repairs")]
    [InlineData("Door & Window Repairs")]
    [InlineData("Cleaning & Janitorial")]
    [InlineData("Gardening & Landscaping")]
    [InlineData("Painting")]
    [InlineData("Pest")]
    [InlineData("Security, Locks & Keys")]
    [InlineData("Other Repairs")]
    public async Task RepairTransaction_AllSubCategories_MappedCorrectly(string subCategory)
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 3, 1), "Repairs & Maintenance", subCategory, -250.00m),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Single(reports[0].RepairLines);
        Assert.Equal(subCategory, reports[0].RepairLines[0].SubCategory);
        Assert.Equal(250.00m, reports[0].RepairLines[0].MonthlyAmounts[2]); // Mar
    }

    [Fact]
    public async Task PositiveRepairAmount_MappedToTenantChargeForRepair()
    {
        // Positive repair amounts (e.g., $30 from tenant) should map to
        // TenantChargeForRepair income, NOT to repair expense lines.
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 10, 15), "Repairs & Maintenance", "Other Repairs", 30.00m, name: "April N. Castellanos"),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        // Should go to TenantChargeForRepair income
        Assert.Equal(30.00m, reports[0].TenantChargeForRepair[9]); // Oct
        // Repair lines should be empty
        Assert.Empty(reports[0].RepairLines);
    }

    [Fact]
    public async Task PositiveRepairAmount_LargerAmount_MappedToTenantChargeForRepair()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 11, 10), "Repairs & Maintenance", "Plumbing Repairs", 150.00m, name: "April N. Castellanos"),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(150.00m, reports[0].TenantChargeForRepair[10]); // Nov
        Assert.Empty(reports[0].RepairLines);
    }

    [Fact]
    public async Task MixedRepairAmounts_PositiveToIncome_NegativeToExpense()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 10, 17), "Repairs & Maintenance", "Plumbing Repairs", -125.00m, name: "Arc Home Solutions"),
            CreateTransaction(new DateTime(2025, 10, 15), "Repairs & Maintenance", "Other Repairs", 30.00m, name: "April N. Castellanos"),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        // Positive → TenantChargeForRepair
        Assert.Equal(30.00m, reports[0].TenantChargeForRepair[9]);
        // Negative → RepairLines
        Assert.Single(reports[0].RepairLines);
        Assert.Equal("Plumbing Repairs", reports[0].RepairLines[0].SubCategory);
        Assert.Equal(125.00m, reports[0].RepairLines[0].MonthlyAmounts[9]);
    }

    // ===== CAPITAL EXPENSES TESTS =====

    [Fact]
    public async Task CapitalExpenses_MappedCorrectly()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 7, 1), "Capital Expenses", "New Appliances", -1200.00m),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Single(reports[0].CapitalExpenses);
        Assert.Equal("New Appliances", reports[0].CapitalExpenses[0].SubCategory);
        Assert.Equal(1200.00m, reports[0].CapitalExpenses[0].MonthlyAmounts[6]); // Jul
    }

    [Fact]
    public async Task CapitalExpenses_NullSubCategory_MappedAsCapitalExpenses()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 4, 1), "Capital Expenses", null, -500),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Single(reports[0].CapitalExpenses);
        Assert.Equal("Capital Expenses", reports[0].CapitalExpenses[0].SubCategory);
    }

    // ===== LEGAL & PROFESSIONAL TESTS =====

    [Fact]
    public async Task LegalProfessional_MappedToOtherProfessionalServices()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 5, 1), "Legal & Professional", "Inspections", -150),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(150, reports[0].OtherProfessionalServices[4]); // May
    }

    // ===== UTILITIES TESTS =====

    [Fact]
    public async Task Utilities_MappedCorrectly()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 8, 1), "Utilities", "Electric", -85),
            CreateTransaction(new DateTime(2025, 8, 15), "Utilities", "Water & Sewer", -45),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(130, reports[0].Utilities[7]); // Aug (85 + 45)
    }

    // ===== TRANSFERS TESTS =====

    [Fact]
    public async Task EscrowPayments_MappedCorrectly()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 1, 1), "Transfers", "General Escrow Payments", -626.51m),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(626.51m, reports[0].EscrowPayments[0]); // Jan
    }

    [Fact]
    public async Task OwnerDistributions_NotMappedToPnL()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 1, 1), "Transfers", "Owner Distributions", -5000),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        // Should not appear in any P&L category
        Assert.Equal(0, reports[0].TotalIncome);
        Assert.Equal(0, reports[0].TotalExpenses);
        Assert.Equal(0, reports[0].EscrowPayments.Sum());
        Assert.Equal(0, reports[0].MortgagePayment.Sum());
    }

    [Fact]
    public async Task TransfersNullSubCategory_NotMapped()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 1, 1), "Transfers", null, -1380.12m),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        // Transfers with NULL subcategory fall through the switch
        Assert.Equal(0, reports[0].MortgagePayment.Sum());
        Assert.Equal(0, reports[0].EscrowPayments.Sum());
    }

    [Fact]
    public async Task SecurityDeposits_NotMappedToPnL()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 5, 1), "Security Deposits", null, -1000),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(0, reports[0].TotalIncome);
        Assert.Equal(0, reports[0].TotalExpenses);
    }

    // ===== NULL CATEGORY / MORTGAGE AUTO-DEBIT TESTS =====

    [Fact]
    public async Task NullCategory_MatchingMortgageAmount_MappedToMortgagePayment()
    {
        var property = CreateTestProperty();
        var lender = new Lender { MonthlyPayment = 1380.12m };
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 1, 1), null, null, -1380.12m),
        };
        SetupMocks(property, transactions, lender: lender);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(1380.12m, reports[0].MortgagePayment[0]); // Jan
    }

    [Fact]
    public async Task NullCategory_NonMatchingMortgageAmount_NotMapped()
    {
        var property = CreateTestProperty();
        var lender = new Lender { MonthlyPayment = 1380.12m };
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 1, 1), null, null, -500.00m),
        };
        SetupMocks(property, transactions, lender: lender);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(0, reports[0].MortgagePayment[0]); // Not matched
    }

    // ===== TOTALS / COMPUTED PROPERTIES TESTS =====

    [Fact]
    public async Task TotalIncome_CalculatedCorrectly()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 1, 1), "Income", "Rents", 1500),
            CreateTransaction(new DateTime(2025, 2, 1), "Income", "Rents", 1500),
            CreateTransaction(new DateTime(2025, 1, 5), "Income", "Pet Fees", 50),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(3050m, reports[0].TotalIncome);
    }

    [Fact]
    public async Task TotalExpenses_IncludesAllExpenseCategories()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 1, 1), "Management Fees", "Property Management", -100),
            CreateTransaction(new DateTime(2025, 1, 1), "Mortgages & Loans", "Mortgage Interest", -200),
            CreateTransaction(new DateTime(2025, 1, 1), "Repairs & Maintenance", "Plumbing Repairs", -300),
            CreateTransaction(new DateTime(2025, 1, 1), "Utilities", "Electric", -50),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(650m, reports[0].TotalExpenses); // 100 + 200 + 300 + 50
    }

    [Fact]
    public async Task NetIncome_IsIncomeMinusExpenses()
    {
        var property = CreateTestProperty();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateTime(2025, 1, 1), "Income", "Rents", 2000),
            CreateTransaction(new DateTime(2025, 1, 1), "Management Fees", "Property Management", -200),
            CreateTransaction(new DateTime(2025, 1, 1), "Repairs & Maintenance", "Plumbing Repairs", -100),
        };
        SetupMocks(property, transactions);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal(2000m, reports[0].TotalIncome);
        Assert.Equal(300m, reports[0].TotalExpenses);
        Assert.Equal(1700m, reports[0].NetIncome);
    }

    // ===== PROPERTY LIST & YEARS TESTS =====

    [Fact]
    public async Task GetPropertyList_ReturnsOrderedByShortName()
    {
        var properties = new[]
        {
            CreateTestProperty(1, "Zebra St"),
            CreateTestProperty(2, "Alpha St"),
            CreateTestProperty(3, "Middle St"),
        };
        _propertyRepoMock.Setup(r => r.GetAllPropertiesAsync()).ReturnsAsync(properties);

        var result = await _sut.GetPropertyListAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal("Alpha St", result[0].ShortName);
        Assert.Equal("Middle St", result[1].ShortName);
        Assert.Equal("Zebra St", result[2].ShortName);
    }

    [Fact]
    public async Task GetAvailableYears_ReturnsDescending()
    {
        _transactionRepoMock.Setup(r => r.GetDistinctYearsAsync())
            .ReturnsAsync(new[] { 2023, 2025, 2024 });

        var result = await _sut.GetAvailableYearsAsync();

        Assert.Equal(new[] { 2025, 2024, 2023 }, result);
    }

    // ===== PROPERTY FILTER TESTS =====

    [Fact]
    public async Task GenerateReport_WithPropertyIds_FiltersCorrectly()
    {
        var props = new[]
        {
            CreateTestProperty(1, "Alpha St"),
            CreateTestProperty(2, "Beta St"),
            CreateTestProperty(3, "Gamma St"),
        };
        _propertyRepoMock.Setup(r => r.GetAllPropertiesAsync()).ReturnsAsync(props);
        _transactionRepoMock.Setup(r => r.GetTransactionsByPropertyAndYearAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Transaction>());
        _lenderRepoMock.Setup(r => r.GetLenderByPropertyIdAsync(It.IsAny<int>())).ReturnsAsync((Lender?)null);
        _hoaRepoMock.Setup(r => r.GetHoaByPropertyIdAsync(It.IsAny<int>())).ReturnsAsync((HoaInfo?)null);
        _insuranceRepoMock.Setup(r => r.GetPremiumByPropertyAndYearAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((InsurancePremium?)null);

        var result = await _sut.GenerateReportDataAsync(2025, new List<int> { 1, 3 });

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.SheetName == "Alpha St");
        Assert.Contains(result, r => r.SheetName == "Gamma St");
    }

    [Fact]
    public async Task GenerateReport_NoPropertyIds_ReturnsAll()
    {
        var props = new[]
        {
            CreateTestProperty(1, "Alpha St"),
            CreateTestProperty(2, "Beta St"),
        };
        _propertyRepoMock.Setup(r => r.GetAllPropertiesAsync()).ReturnsAsync(props);
        _transactionRepoMock.Setup(r => r.GetTransactionsByPropertyAndYearAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Transaction>());
        _lenderRepoMock.Setup(r => r.GetLenderByPropertyIdAsync(It.IsAny<int>())).ReturnsAsync((Lender?)null);
        _hoaRepoMock.Setup(r => r.GetHoaByPropertyIdAsync(It.IsAny<int>())).ReturnsAsync((HoaInfo?)null);
        _insuranceRepoMock.Setup(r => r.GetPremiumByPropertyAndYearAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((InsurancePremium?)null);

        var result = await _sut.GenerateReportDataAsync(2025);

        Assert.Equal(2, result.Count);
    }

    // ===== FULL SCENARIO: Rattler 2025 =====

    [Fact]
    public async Task FullScenario_Rattler2025_MatchesExpectedTotals()
    {
        var property = CreateTestProperty(22, "539 Rattler Bluff");
        var lender = new Lender { MonthlyPayment = 0 }; // No mortgage for this scenario

        var transactions = new List<Transaction>
        {
            // Rent
            CreateTransaction(new DateTime(2025, 1, 15), "Income", "Rents", 1785),
            CreateTransaction(new DateTime(2025, 2, 15), "Income", "Rents", 2690),
            CreateTransaction(new DateTime(2025, 3, 15), "Income", "Rents", 880),
            CreateTransaction(new DateTime(2025, 4, 15), "Income", "Rents", 1785),
            CreateTransaction(new DateTime(2025, 5, 15), "Income", "Rents", 1785),
            CreateTransaction(new DateTime(2025, 6, 15), "Income", "Rents", 1785),
            CreateTransaction(new DateTime(2025, 7, 15), "Income", "Rents", 1785),
            CreateTransaction(new DateTime(2025, 8, 15), "Income", "Rents", 1785),
            CreateTransaction(new DateTime(2025, 9, 15), "Income", "Rents", 1785),
            CreateTransaction(new DateTime(2025, 10, 15), "Income", "Rents", 1785),
            CreateTransaction(new DateTime(2025, 11, 15), "Income", "Rents", 2880),
            // Management Fees (8% per month)
            CreateTransaction(new DateTime(2025, 1, 31), "Management Fees", "Property Management", -142.80m),
            CreateTransaction(new DateTime(2025, 2, 28), "Management Fees", "Property Management", -142.80m),
            CreateTransaction(new DateTime(2025, 3, 31), "Management Fees", "Property Management", -142.80m),
            CreateTransaction(new DateTime(2025, 4, 30), "Management Fees", "Property Management", -142.80m),
            CreateTransaction(new DateTime(2025, 5, 31), "Management Fees", "Property Management", -142.80m),
            CreateTransaction(new DateTime(2025, 6, 30), "Management Fees", "Property Management", -142.80m),
            CreateTransaction(new DateTime(2025, 7, 31), "Management Fees", "Property Management", -142.80m),
            CreateTransaction(new DateTime(2025, 8, 31), "Management Fees", "Property Management", -142.80m),
            CreateTransaction(new DateTime(2025, 9, 30), "Management Fees", "Property Management", -142.80m),
            CreateTransaction(new DateTime(2025, 10, 31), "Management Fees", "Property Management", -142.80m),
            CreateTransaction(new DateTime(2025, 11, 30), "Management Fees", "Property Management", -142.80m),
            CreateTransaction(new DateTime(2025, 12, 31), "Management Fees", "Property Management", -142.80m),
            // Repairs
            CreateTransaction(new DateTime(2025, 1, 31), "Repairs & Maintenance", "Plumbing Repairs", -156.20m),
            CreateTransaction(new DateTime(2025, 6, 5), "Repairs & Maintenance", "Roof Repairs", -325.00m),
            CreateTransaction(new DateTime(2025, 10, 17), "Repairs & Maintenance", null, -135.00m),
            CreateTransaction(new DateTime(2025, 11, 5), "Repairs & Maintenance", null, -33.11m),
        };

        SetupMocks(property, transactions, lender: lender);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 22 });

        var report = reports[0];
        Assert.Equal(20730m, report.Rent.Sum());
        Assert.Equal(1713.60m, report.ManagementFee.Sum());
        Assert.Equal(156.20m, report.RepairLines.First(r => r.SubCategory == "Plumbing Repairs").Total);
        Assert.Equal(325.00m, report.RepairLines.First(r => r.SubCategory == "Roof Repairs").Total);
        Assert.Equal(168.11m, report.RepairLines.First(r => r.SubCategory == "General Repairs").Total);
    }

    // ===== REPORT METADATA TESTS =====

    [Fact]
    public async Task Report_ContainsLenderAndHoaInfo()
    {
        var property = CreateTestProperty();
        var lender = new Lender
        {
            LenderName = "Chase Bank",
            MonthlyPayment = 1500
        };
        var hoa = new HoaInfo
        {
            HOAName = "Sunrise HOA",
            PaymentAmount = 75,
            PaymentFrequency = "Quarterly"
        };
        SetupMocks(property, new List<Transaction>(), lender: lender, hoa: hoa);

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal("Chase Bank", reports[0].LenderName);
        Assert.Equal(1500m, reports[0].MonthlyMortgagePayment);
        Assert.Equal("Sunrise HOA", reports[0].HoaName);
        Assert.Equal(75m, reports[0].HoaPaymentAmount);
        Assert.Equal("Quarterly", reports[0].HoaFrequency);
    }

    [Fact]
    public async Task Report_SheetName_IsStreetName()
    {
        var property = CreateTestProperty(1, "123 Main St");
        SetupMocks(property, new List<Transaction>());

        var reports = await _sut.GenerateReportDataAsync(2025, new List<int> { 1 });

        Assert.Equal("123 Main St", reports[0].SheetName);
    }
}
