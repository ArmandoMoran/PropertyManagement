namespace PropertyManagement.Application.DTOs;

public class PropertyListItemDto
{
    public int PropertyId { get; set; }
    public string FullAddress { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
}
