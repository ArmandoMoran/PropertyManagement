using Dapper;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Interfaces;
using PropertyManagement.Infrastructure.Data;

namespace PropertyManagement.Infrastructure.Repositories;

public class LenderRepository : ILenderRepository
{
    private readonly IDbConnectionFactory _factory;

    public LenderRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<Lender?> GetLenderByPropertyIdAsync(int propertyId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Lender>(
            @"SELECT TOP 1 LenderId, PropertyId, LenderName, LenderUrl, UserId, MortgageNumber, MonthlyPayment, EffectiveDate
              FROM Lenders WHERE PropertyId = @PropertyId
              ORDER BY EffectiveDate DESC",
            new { PropertyId = propertyId });
    }

    public async Task<IEnumerable<Lender>> GetAllLendersByPropertyIdAsync(int propertyId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryAsync<Lender>(
            @"SELECT LenderId, PropertyId, LenderName, LenderUrl, UserId, MortgageNumber, MonthlyPayment, EffectiveDate
              FROM Lenders WHERE PropertyId = @PropertyId
              ORDER BY EffectiveDate DESC",
            new { PropertyId = propertyId });
    }

    public async Task<int> CreateLenderAsync(Lender lender)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleAsync<int>(
            @"INSERT INTO Lenders (PropertyId, LenderName, LenderUrl, UserId, MortgageNumber, MonthlyPayment, EffectiveDate)
              OUTPUT INSERTED.LenderId
              VALUES (@PropertyId, @LenderName, @LenderUrl, @UserId, @MortgageNumber, @MonthlyPayment, @EffectiveDate)",
            lender);
    }

    public async Task<bool> UpdateLenderAsync(Lender lender)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.ExecuteAsync(
            @"UPDATE Lenders SET LenderName=@LenderName, LenderUrl=@LenderUrl, UserId=@UserId,
              MortgageNumber=@MortgageNumber, MonthlyPayment=@MonthlyPayment, EffectiveDate=@EffectiveDate
              WHERE LenderId=@LenderId",
            lender);
        return rows > 0;
    }

    public async Task<bool> DeleteLenderAsync(int lenderId)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.ExecuteAsync("DELETE FROM Lenders WHERE LenderId=@Id", new { Id = lenderId });
        return rows > 0;
    }

    public async Task<IEnumerable<PrincipalBalanceHistory>> GetPrincipalBalanceHistoryAsync(int propertyId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryAsync<PrincipalBalanceHistory>(
            @"SELECT BalanceId, PropertyId, SnapshotDate, PrincipalBalance, CreatedDate
              FROM PrincipalBalanceHistory WHERE PropertyId=@PropertyId ORDER BY SnapshotDate DESC",
            new { PropertyId = propertyId });
    }

    public async Task<int> CreatePrincipalBalanceAsync(PrincipalBalanceHistory balance)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleAsync<int>(
            @"INSERT INTO PrincipalBalanceHistory (PropertyId, SnapshotDate, PrincipalBalance)
              OUTPUT INSERTED.BalanceId
              VALUES (@PropertyId, @SnapshotDate, @PrincipalBalance)",
            balance);
    }
}
