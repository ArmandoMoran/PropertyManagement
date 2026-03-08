using Microsoft.Extensions.DependencyInjection;
using PropertyManagement.Application.Interfaces;
using PropertyManagement.Application.Services;
using PropertyManagement.Domain.Interfaces;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Infrastructure.Excel;
using PropertyManagement.Infrastructure.Repositories;

namespace PropertyManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));

        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ILenderRepository, LenderRepository>();
        services.AddScoped<IHoaRepository, HoaRepository>();
        services.AddScoped<IInsuranceRepository, InsuranceRepository>();

        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IExcelReportGenerator, ExcelReportGenerator>();

        return services;
    }
}
