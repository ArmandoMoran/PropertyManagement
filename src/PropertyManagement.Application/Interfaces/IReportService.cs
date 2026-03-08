using PropertyManagement.Application.DTOs;

namespace PropertyManagement.Application.Interfaces;

public interface IReportService
{
    Task<List<PropertyReportDto>> GenerateReportDataAsync(int year, List<int>? propertyIds = null);
    Task<List<PropertyListItemDto>> GetPropertyListAsync();
    Task<List<int>> GetAvailableYearsAsync();
}
