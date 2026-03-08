using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Interfaces;

namespace PropertyManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IExcelReportGenerator _excelGenerator;

    public ReportsController(IReportService reportService, IExcelReportGenerator excelGenerator)
    {
        _reportService = reportService;
        _excelGenerator = excelGenerator;
    }

    /// <summary>
    /// Generate report data (JSON) for a given year and optional property filter
    /// </summary>
    [HttpGet("{year}")]
    public async Task<ActionResult<List<PropertyReportDto>>> GetReportData(
        int year,
        [FromQuery] string? property = null)
    {
        List<int>? propertyIds = null;

        if (!string.IsNullOrEmpty(property) && property.ToLower() != "all")
        {
            propertyIds = await ResolvePropertyIdsAsync(property);
            if (propertyIds.Count == 0)
                return NotFound($"No properties found matching '{property}'");
        }

        var reports = await _reportService.GenerateReportDataAsync(year, propertyIds);
        return Ok(reports);
    }

    /// <summary>
    /// Generate and download Excel workbook for a given year and optional property filter
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{year}/excel")]
    public async Task<IActionResult> DownloadExcel(
        int year,
        [FromQuery] string? property = null)
    {
        List<int>? propertyIds = null;

        if (!string.IsNullOrEmpty(property) && property.ToLower() != "all")
        {
            propertyIds = await ResolvePropertyIdsAsync(property);
            if (propertyIds.Count == 0)
                return NotFound($"No properties found matching '{property}'");
        }

        var reports = await _reportService.GenerateReportDataAsync(year, propertyIds);

        if (reports.Count == 0)
            return NotFound("No data found for the specified criteria.");

        var excelBytes = await _excelGenerator.GenerateWorkbookAsync(reports, year);

        var fileName = propertyIds != null && propertyIds.Count == 1
            ? $"PropertyReport_{reports.First().SheetName}_{year}.xlsx"
            : $"PropertyReport_All_{year}.xlsx";

        return File(excelBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private async Task<List<int>> ResolvePropertyIdsAsync(string propertyFilter)
    {
        var allProperties = await _reportService.GetPropertyListAsync();

        // Support comma-separated property names
        var filters = propertyFilter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var matchedIds = new List<int>();

        foreach (var filter in filters)
        {
            // Try to parse as ID first
            if (int.TryParse(filter, out int id))
            {
                matchedIds.Add(id);
                continue;
            }

            // Search by name (case-insensitive partial match)
            var matches = allProperties
                .Where(p => p.ShortName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                         || p.FullAddress.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.PropertyId)
                .ToList();

            matchedIds.AddRange(matches);
        }

        return matchedIds.Distinct().ToList();
    }
}
