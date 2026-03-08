using Dapper;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Interfaces;
using PropertyManagement.Infrastructure.Data;

namespace PropertyManagement.Infrastructure.Repositories;

public class InsuranceRepository : IInsuranceRepository
{
    private readonly IDbConnectionFactory _factory;

    public InsuranceRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<Insurance?> GetInsuranceByPropertyIdAsync(int propertyId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Insurance>(
            @"SELECT TOP 1 InsuranceId, PropertyId, Carrier, PolicyNumber, RenewalDate, WhoPays
              FROM Insurance WHERE PropertyId = @PropertyId",
            new { PropertyId = propertyId });
    }

    public async Task<InsurancePremium?> GetPremiumByPropertyAndYearAsync(int propertyId, int year)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<InsurancePremium>(
            @"SELECT TOP 1 iph.PremiumId, iph.InsuranceId, iph.PolicyYear, iph.AnnualPremium, iph.YOYPercentChange
              FROM InsurancePremiumHistory iph
              INNER JOIN Insurance i ON iph.InsuranceId = i.InsuranceId
              WHERE i.PropertyId = @PropertyId AND iph.PolicyYear = @Year",
            new { PropertyId = propertyId, Year = year });
    }

    public async Task<IEnumerable<InsurancePremium>> GetAllPremiumsByPropertyAsync(int propertyId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryAsync<InsurancePremium>(
            @"SELECT iph.PremiumId, iph.InsuranceId, iph.PolicyYear, iph.AnnualPremium, iph.YOYPercentChange
              FROM InsurancePremiumHistory iph
              INNER JOIN Insurance i ON iph.InsuranceId = i.InsuranceId
              WHERE i.PropertyId = @PropertyId
              ORDER BY iph.PolicyYear DESC",
            new { PropertyId = propertyId });
    }

    public async Task<int> CreateInsuranceAsync(Insurance ins)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleAsync<int>(
            @"INSERT INTO Insurance (PropertyId, Carrier, PolicyNumber, RenewalDate, WhoPays)
              OUTPUT INSERTED.InsuranceId
              VALUES (@PropertyId, @Carrier, @PolicyNumber, @RenewalDate, @WhoPays)",
            ins);
    }

    public async Task<bool> UpdateInsuranceAsync(Insurance ins)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.ExecuteAsync(
            @"UPDATE Insurance SET Carrier=@Carrier, PolicyNumber=@PolicyNumber, RenewalDate=@RenewalDate, WhoPays=@WhoPays
              WHERE InsuranceId=@InsuranceId",
            ins);
        return rows > 0;
    }

    public async Task<int> CreatePremiumAsync(InsurancePremium prem)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleAsync<int>(
            @"INSERT INTO InsurancePremiumHistory (InsuranceId, PolicyYear, AnnualPremium, YOYPercentChange)
              OUTPUT INSERTED.PremiumId
              VALUES (@InsuranceId, @PolicyYear, @AnnualPremium, @YOYPercentChange)",
            prem);
    }
}
