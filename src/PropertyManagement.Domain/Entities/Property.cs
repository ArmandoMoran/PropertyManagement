namespace PropertyManagement.Domain.Entities;

public class Property
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

    public string DisplayName => $"{Street}, {City}, {State} {ZipCode}";
    public string ShortName => Street;
}

public class PrincipalBalanceHistory
{
    public int BalanceId { get; set; }
    public int PropertyId { get; set; }
    public DateTime SnapshotDate { get; set; }
    public decimal PrincipalBalance { get; set; }
    public DateTime? CreatedDate { get; set; }
}

public class PropertyHistory
{
    public int HistoryId { get; set; }
    public int PropertyId { get; set; }
    public DateTime EventDate { get; set; }
    public string? PropertyName { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedDate { get; set; }
}
