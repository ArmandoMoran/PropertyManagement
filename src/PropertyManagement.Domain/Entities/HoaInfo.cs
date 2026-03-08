namespace PropertyManagement.Domain.Entities;

public class HoaInfo
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
