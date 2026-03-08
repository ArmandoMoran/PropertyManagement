using Dapper;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Interfaces;
using PropertyManagement.Infrastructure.Data;

namespace PropertyManagement.Infrastructure.Repositories;

public class HoaRepository : IHoaRepository
{
    private readonly IDbConnectionFactory _factory;

    public HoaRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<HoaInfo?> GetHoaByPropertyIdAsync(int propertyId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<HoaInfo>(
            @"SELECT TOP 1 HOAId, PropertyId, HOAName, AccountNumber, ManagementCompany, 
                     PaymentFrequency, PaymentAmount, EffectiveYear
              FROM HOA WHERE PropertyId = @PropertyId
              ORDER BY EffectiveYear DESC",
            new { PropertyId = propertyId });
    }

    public async Task<IEnumerable<HoaInfo>> GetAllHoaByPropertyIdAsync(int propertyId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryAsync<HoaInfo>(
            @"SELECT HOAId, PropertyId, HOAName, AccountNumber, ManagementCompany, 
                     PaymentFrequency, PaymentAmount, EffectiveYear
              FROM HOA WHERE PropertyId = @PropertyId
              ORDER BY EffectiveYear DESC",
            new { PropertyId = propertyId });
    }

    public async Task<int> CreateHoaAsync(HoaInfo hoa)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleAsync<int>(
            @"INSERT INTO HOA (PropertyId, HOAName, AccountNumber, ManagementCompany, PaymentFrequency, PaymentAmount, EffectiveYear)
              OUTPUT INSERTED.HOAId
              VALUES (@PropertyId, @HOAName, @AccountNumber, @ManagementCompany, @PaymentFrequency, @PaymentAmount, @EffectiveYear)",
            hoa);
    }

    public async Task<bool> UpdateHoaAsync(HoaInfo hoa)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.ExecuteAsync(
            @"UPDATE HOA SET HOAName=@HOAName, AccountNumber=@AccountNumber, ManagementCompany=@ManagementCompany,
              PaymentFrequency=@PaymentFrequency, PaymentAmount=@PaymentAmount, EffectiveYear=@EffectiveYear
              WHERE HOAId=@HOAId",
            hoa);
        return rows > 0;
    }

    public async Task<bool> DeleteHoaAsync(int hoaId)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.ExecuteAsync("DELETE FROM HOA WHERE HOAId=@Id", new { Id = hoaId });
        return rows > 0;
    }
}
