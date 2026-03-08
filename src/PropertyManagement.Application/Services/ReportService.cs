using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Interfaces;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Interfaces;
using Microsoft.Data.SqlClient;
using Dapper;

namespace PropertyManagement.Application.Services;

public class ReportService : IReportService
{
    private readonly IPropertyRepository _propertyRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ILenderRepository _lenderRepo;
    private readonly IHoaRepository _hoaRepo;
    private readonly IInsuranceRepository _insuranceRepo;

    public ReportService(
        IPropertyRepository propertyRepo,
        ITransactionRepository transactionRepo,
        ILenderRepository lenderRepo,
        IHoaRepository hoaRepo,
        IInsuranceRepository insuranceRepo)
    {
        _propertyRepo = propertyRepo;
        _transactionRepo = transactionRepo;
        _lenderRepo = lenderRepo;
        _hoaRepo = hoaRepo;
        _insuranceRepo = insuranceRepo;
    }

    public async Task<List<PropertyListItemDto>> GetPropertyListAsync()
    {
        var properties = await _propertyRepo.GetAllPropertiesAsync();
        return properties.Select(p => new PropertyListItemDto
        {
            PropertyId = p.PropertyId,
            FullAddress = p.FullAddress,
            ShortName = p.ShortName
        }).OrderBy(p => p.ShortName).ToList();
    }

    public async Task<List<int>> GetAvailableYearsAsync()
    {
        var years = await _transactionRepo.GetDistinctYearsAsync();
        return years.OrderByDescending(y => y).ToList();
    }

    public async Task<List<PropertyReportDto>> GenerateReportDataAsync(int year, List<int>? propertyIds = null)
    {
        IEnumerable<Property> properties;

        if (propertyIds != null && propertyIds.Count > 0)
        {
            var allProps = await _propertyRepo.GetAllPropertiesAsync();
            properties = allProps.Where(p => propertyIds.Contains(p.PropertyId));
        }
        else
        {
            properties = await _propertyRepo.GetAllPropertiesAsync();
        }

        var reports = new List<PropertyReportDto>();

        foreach (var property in properties)
        {
            var report = await BuildPropertyReportAsync(property, year);
            reports.Add(report);
        }

        return reports.OrderBy(r => r.SheetName).ToList();
    }

    private async Task<PropertyReportDto> BuildPropertyReportAsync(Property property, int year)
    {
        var transactions = await _transactionRepo.GetTransactionsByPropertyAndYearAsync(property.PropertyId, year);
        var lender = await _lenderRepo.GetLenderByPropertyIdAsync(property.PropertyId);
        var hoa = await _hoaRepo.GetHoaByPropertyIdAsync(property.PropertyId);
        var insurancePremium = await _insuranceRepo.GetPremiumByPropertyAndYearAsync(property.PropertyId, year);

        var report = new PropertyReportDto
        {
            PropertyId = property.PropertyId,
            PropertyAddress = property.DisplayName,
            SheetName = $"{property.Street}",
            LenderName = lender?.LenderName,
            MonthlyMortgagePayment = lender?.MonthlyPayment ?? 0,
            HoaName = hoa?.HOAName,
            HoaPaymentAmount = hoa?.PaymentAmount ?? 0,
            HoaFrequency = hoa?.PaymentFrequency,
            Year = year
        };

        try
        {
            using var conn = new SqlConnection("Server=localhost;Database=PropertyManagement;Trusted_Connection=True;TrustServerCertificate=True;");
            report.InterestPaid1098 = await conn.QueryFirstOrDefaultAsync<decimal>(
                "SELECT MortgageInterest FROM Tax1098 WHERE PropertyId = @pid AND TaxYear = @yr",
                new { pid = property.PropertyId, yr = year }
            );
            var pm = await conn.QueryFirstOrDefaultAsync<(decimal TotalOperatingIncome, decimal ManagementFee)>(
                "SELECT SUM(TotalOperatingIncome), SUM(ManagementFee) FROM PMStatement WHERE PropertyId = @pid AND TaxYear = @yr",
                new { pid = property.PropertyId, yr = year }
            );
            report.PMTotalIncome = pm.TotalOperatingIncome;
            report.PMManagementFee = pm.ManagementFee;
        }
        catch { }

        foreach (var txn in transactions)
        {
            int monthIndex = txn.TransactionDate.Month - 1; // 0-based
            decimal amount = Math.Abs(txn.Amount); // Expenses stored as negative, we use absolute

            MapTransaction(report, txn, monthIndex, amount);
        }

        // If no insurance transactions found, use the premium from InsurancePremiumHistory
        if (report.InsurancePremium.All(v => v == 0) && insurancePremium != null)
        {
            // Spread annual premium across months (or put in renewal month)
            // For the spreadsheet, we typically show the annual amount in one cell
            // Let's check if there's a renewal date to place it
            var insurance = await _insuranceRepo.GetInsuranceByPropertyIdAsync(property.PropertyId);
            if (insurance?.RenewalDate != null)
            {
                int renewalMonth = insurance.RenewalDate.Value.Month - 1;
                report.InsurancePremium[renewalMonth] = insurancePremium.AnnualPremium;
            }
            else
            {
                // Put in January
                report.InsurancePremium[0] = insurancePremium.AnnualPremium;
            }
        }

        return report;
    }

    private void MapTransaction(PropertyReportDto report, Transaction txn, int monthIndex, decimal absAmount)
    {
        var category = txn.Category ?? "";
        var subCategory = txn.SubCategory ?? "";
        bool isExpense = txn.Amount < 0;
        decimal signedAmount = txn.Amount;

        switch (category)
        {
            case "Income":
                switch (subCategory)
                {
                    case "Rents":
                    case "Section 8 Rents":
                        report.Rent[monthIndex] += signedAmount;
                        break;
                    case "Pet Fees":
                        report.PetFees[monthIndex] += signedAmount;
                        break;
                    default:
                        // Any other income
                        report.OtherIncome[monthIndex] += signedAmount;
                        break;
                }
                break;

            case "Management Fees":
                switch (subCategory)
                {
                    case "Leasing Commissions":
                        report.LeasingCommissions[monthIndex] += absAmount;
                        break;
                    case "Property Management":
                    default:
                        report.ManagementFee[monthIndex] += absAmount;
                        break;
                }
                break;

            case "Mortgages & Loans":
                switch (subCategory)
                {
                    case "Mortgage Interest":
                        report.MortgageInterest[monthIndex] += absAmount;
                        break;
                    case "Mortgage Principal":
                        report.MortgagePrincipal[monthIndex] += absAmount;
                        break;
                    case "Mortgage Payment":
                        report.MortgagePayment[monthIndex] += absAmount;
                        break;
                    default:
                        // Other Loan Payment - treat as mortgage payment
                        report.MortgagePayment[monthIndex] += absAmount;
                        break;
                }
                break;

            case "Taxes":
                // Property taxes can be positive (escrow refund) or negative (payment)
                // We store as positive expense amount
                if (txn.Amount > 0)
                    report.PropertyTaxes[monthIndex] += txn.Amount; // positive tax values (escrow disbursement)
                else
                    report.PropertyTaxes[monthIndex] += absAmount;
                break;

            case "Insurance":
                report.InsurancePremium[monthIndex] += absAmount;
                break;

            case "Admin & Other":
                if (subCategory == "HOA Dues")
                    report.HoaDues[monthIndex] += absAmount;
                else
                    report.OtherIncome[monthIndex] += Math.Abs(txn.Amount);
                break;

            case "Repairs & Maintenance":
                if (txn.Amount > 0)
                {
                    // Positive amounts are tenant charges for repair (income)
                    report.TenantChargeForRepair[monthIndex] += txn.Amount;
                }
                else
                {
                    MapRepairTransaction(report, subCategory, monthIndex, absAmount);
                }
                break;

            case "Capital Expenses":
                MapCapitalExpense(report, subCategory, monthIndex, absAmount);
                break;

            case "Legal & Professional":
                report.OtherProfessionalServices[monthIndex] += absAmount;
                break;

            case "Utilities":
                report.Utilities[monthIndex] += absAmount;
                break;

            case "Security Deposits":
            case "Transfers":
                // Transfers include escrow payments and owner distributions
                switch (subCategory)
                {
                    case "General Escrow Payments":
                        report.EscrowPayments[monthIndex] += absAmount;
                        break;
                    // Owner Distributions, Credit Card Payments, Owner Contributions
                    // are not part of the P&L report
                }
                break;

            default:
                // NULL category - check if it looks like a mortgage payment
                if (string.IsNullOrEmpty(category) && Math.Abs(txn.Amount) > 0)
                {
                    // These are typically auto-debits for mortgage
                    // Skip or treat as mortgage payment based on amount
                    if (absAmount == report.MonthlyMortgagePayment && report.MonthlyMortgagePayment > 0)
                    {
                        report.MortgagePayment[monthIndex] += absAmount;
                    }
                }
                break;
        }
    }

    private void MapRepairTransaction(PropertyReportDto report, string subCategory, int monthIndex, decimal amount)
    {
        string label = string.IsNullOrEmpty(subCategory) ? "General Repairs" : subCategory;

        var existing = report.RepairLines.FirstOrDefault(r => r.SubCategory == label);
        if (existing == null)
        {
            existing = new RepairLineDto { SubCategory = label };
            report.RepairLines.Add(existing);
        }
        existing.MonthlyAmounts[monthIndex] += amount;
    }

    private void MapCapitalExpense(PropertyReportDto report, string subCategory, int monthIndex, decimal amount)
    {
        string label = string.IsNullOrEmpty(subCategory) ? "Capital Expenses" : subCategory;

        var existing = report.CapitalExpenses.FirstOrDefault(c => c.SubCategory == label);
        if (existing == null)
        {
            existing = new CapitalExpenseLineDto { SubCategory = label };
            report.CapitalExpenses.Add(existing);
        }
        existing.MonthlyAmounts[monthIndex] += amount;
    }
}
