namespace PropertyManagement.Application.DTOs;

public class ReportRequestDto
{
    public int Year { get; set; }
    /// <summary>
    /// Optional list of property IDs. If null or empty, generate for all properties.
    /// </summary>
    public List<int>? PropertyIds { get; set; }
}
