namespace PropertyManagement.Application.DTOs;

public class PropertyDetailDto
{
    public int PropertyId { get; set; }
    public string FullAddress { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string? Owner { get; set; }
    public string? PropertyType { get; set; }
    public int? Units { get; set; }
    public int? SqFt { get; set; }
    public decimal? Zestimate { get; set; }

    public LenderDto? Lender { get; set; }
    public HoaDto? Hoa { get; set; }
    public InsuranceDto? Insurance { get; set; }
    public List<InsurancePremiumDto> InsurancePremiums { get; set; } = new();
    public List<LenderDto> AllLenders { get; set; } = new();
    public List<PrincipalBalanceDto> PrincipalBalanceHistory { get; set; } = new();
    public List<PropertyHistoryDto> PropertyHistory { get; set; } = new();
}

public class LenderDto
{
    public int LenderId { get; set; }
    public int PropertyId { get; set; }
    public string LenderName { get; set; } = string.Empty;
    public string? LenderUrl { get; set; }
    public string? UserId { get; set; }
    public string? MortgageNumber { get; set; }
    public decimal MonthlyPayment { get; set; }
    public DateTime? EffectiveDate { get; set; }
}

public class HoaDto
{
    public int HOAId { get; set; }
    public int PropertyId { get; set; }
    public string HOAName { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public string? ManagementCompany { get; set; }
    public string? PaymentFrequency { get; set; }
    public decimal PaymentAmount { get; set; }
    public int? EffectiveYear { get; set; }
}

public class InsuranceDto
{
    public int InsuranceId { get; set; }
    public int PropertyId { get; set; }
    public string Carrier { get; set; } = string.Empty;
    public string? PolicyNumber { get; set; }
    public DateTime? RenewalDate { get; set; }
    public string? WhoPays { get; set; }
}

public class InsurancePremiumDto
{
    public int PremiumId { get; set; }
    public int InsuranceId { get; set; }
    public int PolicyYear { get; set; }
    public decimal AnnualPremium { get; set; }
    public decimal? YOYPercentChange { get; set; }
}

public class PrincipalBalanceDto
{
    public int BalanceId { get; set; }
    public int PropertyId { get; set; }
    public DateTime SnapshotDate { get; set; }
    public decimal PrincipalBalance { get; set; }
}

public class PropertyHistoryDto
{
    public int HistoryId { get; set; }
    public int PropertyId { get; set; }
    public DateTime EventDate { get; set; }
    public string? PropertyName { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedDate { get; set; }
}
