using Microsoft.AspNetCore.Mvc;
using Moq;
using PropertyManagement.API.Controllers;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Application.Interfaces;

namespace PropertyManagement.Tests.Controllers;

public class ReportsControllerTests
{
    private readonly Mock<IReportService> _reportServiceMock;
    private readonly Mock<IExcelReportGenerator> _excelGeneratorMock;
    private readonly ReportsController _sut;

    public ReportsControllerTests()
    {
        _reportServiceMock = new Mock<IReportService>();
        _excelGeneratorMock = new Mock<IExcelReportGenerator>();
        _sut = new ReportsController(_reportServiceMock.Object, _excelGeneratorMock.Object);
    }

    private List<PropertyListItemDto> CreatePropertyList()
    {
        return new List<PropertyListItemDto>
        {
            new() { PropertyId = 1, ShortName = "539 Rattler Bluff", FullAddress = "539 Rattler Bluff, San Antonio, TX 78253" },
            new() { PropertyId = 2, ShortName = "8322 Sageline", FullAddress = "8322 Sageline, San Antonio, TX 78249" },
            new() { PropertyId = 3, ShortName = "7714 Branston", FullAddress = "7714 Branston, San Antonio, TX 78250" },
        };
    }

    // ===== GetReportData Tests =====

    [Fact]
    public async Task GetReportData_NoFilter_ReturnsAllReports()
    {
        var reports = new List<PropertyReportDto>
        {
            new() { PropertyId = 1, SheetName = "Rattler" },
            new() { PropertyId = 2, SheetName = "Sageline" },
        };
        _reportServiceMock.Setup(s => s.GenerateReportDataAsync(2025, null))
            .ReturnsAsync(reports);

        var result = await _sut.GetReportData(2025, null);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsType<List<PropertyReportDto>>(okResult.Value);
        Assert.Equal(2, data.Count);
    }

    [Fact]
    public async Task GetReportData_AllFilter_ReturnsAllReports()
    {
        var reports = new List<PropertyReportDto>
        {
            new() { PropertyId = 1, SheetName = "Rattler" },
        };
        _reportServiceMock.Setup(s => s.GenerateReportDataAsync(2025, null))
            .ReturnsAsync(reports);

        var result = await _sut.GetReportData(2025, "all");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetReportData_ByNumericId_FiltersCorrectly()
    {
        _reportServiceMock.Setup(s => s.GetPropertyListAsync())
            .ReturnsAsync(CreatePropertyList());
        _reportServiceMock.Setup(s => s.GenerateReportDataAsync(2025, It.Is<List<int>>(l => l.Contains(1))))
            .ReturnsAsync(new List<PropertyReportDto> { new() { PropertyId = 1, SheetName = "Rattler" } });

        var result = await _sut.GetReportData(2025, "1");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsType<List<PropertyReportDto>>(okResult.Value);
        Assert.Single(data);
    }

    [Fact]
    public async Task GetReportData_ByName_PartialMatch()
    {
        _reportServiceMock.Setup(s => s.GetPropertyListAsync())
            .ReturnsAsync(CreatePropertyList());
        _reportServiceMock.Setup(s => s.GenerateReportDataAsync(2025, It.Is<List<int>>(l => l.Contains(1))))
            .ReturnsAsync(new List<PropertyReportDto> { new() { PropertyId = 1, SheetName = "Rattler" } });

        var result = await _sut.GetReportData(2025, "Rattler");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetReportData_NoMatch_ReturnsNotFound()
    {
        _reportServiceMock.Setup(s => s.GetPropertyListAsync())
            .ReturnsAsync(CreatePropertyList());

        var result = await _sut.GetReportData(2025, "NonExistentProperty");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetReportData_CommaSeparatedNames_MultipleMatches()
    {
        _reportServiceMock.Setup(s => s.GetPropertyListAsync())
            .ReturnsAsync(CreatePropertyList());
        _reportServiceMock.Setup(s => s.GenerateReportDataAsync(2025, It.Is<List<int>>(l => l.Count == 2 && l.Contains(1) && l.Contains(2))))
            .ReturnsAsync(new List<PropertyReportDto>
            {
                new() { PropertyId = 1, SheetName = "Rattler" },
                new() { PropertyId = 2, SheetName = "Sageline" },
            });

        var result = await _sut.GetReportData(2025, "Rattler,Sageline");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsType<List<PropertyReportDto>>(okResult.Value);
        Assert.Equal(2, data.Count);
    }

    // ===== DownloadExcel Tests =====

    [Fact]
    public async Task DownloadExcel_ReturnsFileResult()
    {
        var reports = new List<PropertyReportDto>
        {
            new() { PropertyId = 1, SheetName = "Rattler" },
        };
        _reportServiceMock.Setup(s => s.GenerateReportDataAsync(2025, null))
            .ReturnsAsync(reports);
        _excelGeneratorMock.Setup(g => g.GenerateWorkbookAsync(reports, 2025))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        var result = await _sut.DownloadExcel(2025);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        Assert.Contains("PropertyReport_All_2025.xlsx", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task DownloadExcel_SingleProperty_FileNameContainsPropertyName()
    {
        _reportServiceMock.Setup(s => s.GetPropertyListAsync())
            .ReturnsAsync(CreatePropertyList());

        var reports = new List<PropertyReportDto>
        {
            new() { PropertyId = 1, SheetName = "539 Rattler Bluff" },
        };
        _reportServiceMock.Setup(s => s.GenerateReportDataAsync(2025, It.Is<List<int>>(l => l.Count == 1)))
            .ReturnsAsync(reports);
        _excelGeneratorMock.Setup(g => g.GenerateWorkbookAsync(reports, 2025))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        var result = await _sut.DownloadExcel(2025, "Rattler");

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Contains("539 Rattler Bluff", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task DownloadExcel_NoData_ReturnsNotFound()
    {
        _reportServiceMock.Setup(s => s.GenerateReportDataAsync(2025, null))
            .ReturnsAsync(new List<PropertyReportDto>());

        var result = await _sut.DownloadExcel(2025);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DownloadExcel_InvalidProperty_ReturnsNotFound()
    {
        _reportServiceMock.Setup(s => s.GetPropertyListAsync())
            .ReturnsAsync(CreatePropertyList());

        var result = await _sut.DownloadExcel(2025, "DoesNotExist");

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
