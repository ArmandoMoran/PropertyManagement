namespace PropertyManagement.Domain.Entities;

public class Insurance
{
    public int InsuranceId { get; set; }
    public int PropertyId { get; set; }
    public string Carrier { get; set; } = string.Empty;
    public string? PolicyNumber { get; set; }
    public DateTime? RenewalDate { get; set; }
    public string? WhoPays { get; set; }
}

public class InsurancePremium
{
    public int PremiumId { get; set; }
    public int InsuranceId { get; set; }
    public int PolicyYear { get; set; }
    public decimal AnnualPremium { get; set; }
    public decimal? YOYPercentChange { get; set; }
}
