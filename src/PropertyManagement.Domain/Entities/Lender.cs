namespace PropertyManagement.Domain.Entities;

public class Lender
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
