namespace PropertyManagement.Application.Interfaces;

public interface IExcelReportGenerator
{
    Task<byte[]> GenerateWorkbookAsync(List<DTOs.PropertyReportDto> reports, int year);
}
