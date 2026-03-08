using Dapper;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Interfaces;
using PropertyManagement.Infrastructure.Data;

namespace PropertyManagement.Infrastructure.Repositories;

public class PropertyRepository : IPropertyRepository
{
    private readonly IDbConnectionFactory _factory;

    public PropertyRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<Property>> GetAllPropertiesAsync()
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryAsync<Property>(
            "SELECT PropertyId, FullAddress, Street, City, State, ZipCode, Owner, PropertyType, Units, SqFt, Zestimate FROM Properties ORDER BY Street");
    }

    public async Task<Property?> GetPropertyByIdAsync(int propertyId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Property>(
            "SELECT PropertyId, FullAddress, Street, City, State, ZipCode, Owner, PropertyType, Units, SqFt, Zestimate FROM Properties WHERE PropertyId = @PropertyId",
            new { PropertyId = propertyId });
    }

    public async Task<IEnumerable<Property>> SearchPropertiesAsync(string searchTerm)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryAsync<Property>(
            "SELECT PropertyId, FullAddress, Street, City, State, ZipCode, Owner, PropertyType, Units, SqFt, Zestimate FROM Properties WHERE FullAddress LIKE @Search OR Street LIKE @Search ORDER BY Street",
            new { Search = $"%{searchTerm}%" });
    }

    public async Task<int> CreatePropertyAsync(Property property)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleAsync<int>(
            @"INSERT INTO Properties (FullAddress, Street, City, State, ZipCode, Owner, PropertyType, Units, SqFt, Zestimate)
              OUTPUT INSERTED.PropertyId
              VALUES (@FullAddress, @Street, @City, @State, @ZipCode, @Owner, @PropertyType, @Units, @SqFt, @Zestimate)",
            property);
    }

    public async Task<bool> UpdatePropertyAsync(Property property)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.ExecuteAsync(
            @"UPDATE Properties SET FullAddress=@FullAddress, Street=@Street, City=@City, State=@State, 
              ZipCode=@ZipCode, Owner=@Owner, PropertyType=@PropertyType, Units=@Units, SqFt=@SqFt, Zestimate=@Zestimate
              WHERE PropertyId=@PropertyId",
            property);
        return rows > 0;
    }

    public async Task<bool> DeletePropertyAsync(int propertyId)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.ExecuteAsync("DELETE FROM Properties WHERE PropertyId=@PropertyId", new { PropertyId = propertyId });
        return rows > 0;
    }

    public async Task<IEnumerable<PropertyHistory>> GetPropertyHistoryAsync(int propertyId)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryAsync<PropertyHistory>(
            @"SELECT HistoryId, PropertyId, EventDate, PropertyName, Description, Notes, CreatedDate
              FROM PropertyHistory WHERE PropertyId=@PropertyId ORDER BY EventDate DESC",
            new { PropertyId = propertyId });
    }
}
