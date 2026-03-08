namespace PropertyManagement.Domain.Entities;

public class Transaction
{
    public int TransactionId { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Name { get; set; }
    public string? Notes { get; set; }
    public string? Details { get; set; }
    public string? Category { get; set; }
    public string? SubCategory { get; set; }
    public decimal Amount { get; set; }
    public string? Portfolio { get; set; }
    public int? PropertyId { get; set; }
    public string? PropertyRaw { get; set; }
    public string? Unit { get; set; }
    public string? DataSource { get; set; }
    public string? Account { get; set; }
    public string? Owner { get; set; }
}
