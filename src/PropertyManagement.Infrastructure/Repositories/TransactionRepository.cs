using Dapper;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Interfaces;
using PropertyManagement.Infrastructure.Data;

namespace PropertyManagement.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly IDbConnectionFactory _factory;

    public TransactionRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByPropertyAndYearAsync(int propertyId, int year)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryAsync<Transaction>(
            @"SELECT TransactionId, TransactionDate, Name, Notes, Details, Category, SubCategory, 
                     Amount, Portfolio, PropertyId, PropertyRaw, Unit, DataSource, Account, Owner
              FROM Transactions 
              WHERE PropertyId = @PropertyId AND YEAR(TransactionDate) = @Year
              ORDER BY TransactionDate",
            new { PropertyId = propertyId, Year = year });
    }

    public async Task<IEnumerable<Transaction>> GetAllTransactionsByYearAsync(int year)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryAsync<Transaction>(
            @"SELECT TransactionId, TransactionDate, Name, Notes, Details, Category, SubCategory, 
                     Amount, Portfolio, PropertyId, PropertyRaw, Unit, DataSource, Account, Owner
              FROM Transactions 
              WHERE YEAR(TransactionDate) = @Year AND PropertyId IS NOT NULL
              ORDER BY PropertyId, TransactionDate",
            new { Year = year });
    }

    public async Task<IEnumerable<int>> GetDistinctYearsAsync()
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryAsync<int>(
            "SELECT DISTINCT YEAR(TransactionDate) FROM Transactions WHERE PropertyId IS NOT NULL ORDER BY 1");
    }

    public async Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedTransactionsAsync(
        int? propertyId, int page, int pageSize, string? category, string? search,
        DateTime? startDate, DateTime? endDate, string sortBy, bool sortDesc)
    {
        using var conn = _factory.CreateConnection();

        var where = "WHERE 1=1";
        var parameters = new DynamicParameters();

        if (propertyId.HasValue)
        {
            where += " AND t.PropertyId = @PropertyId";
            parameters.Add("PropertyId", propertyId.Value);
        }
        if (!string.IsNullOrEmpty(category))
        {
            where += " AND t.Category = @Category";
            parameters.Add("Category", category);
        }
        if (!string.IsNullOrEmpty(search))
        {
            where += " AND (t.Name LIKE @Search OR t.Notes LIKE @Search OR t.Details LIKE @Search OR t.SubCategory LIKE @Search)";
            parameters.Add("Search", $"%{search}%");
        }
        if (startDate.HasValue)
        {
            where += " AND t.TransactionDate >= @StartDate";
            parameters.Add("StartDate", startDate.Value);
        }
        if (endDate.HasValue)
        {
            where += " AND t.TransactionDate <= @EndDate";
            parameters.Add("EndDate", endDate.Value);
        }

        // Validate sort column
        var allowedSorts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "TransactionDate", "Amount", "Category", "Name", "SubCategory" };
        var orderCol = allowedSorts.Contains(sortBy) ? sortBy : "TransactionDate";
        var orderDir = sortDesc ? "DESC" : "ASC";

        var countSql = $"SELECT COUNT(*) FROM Transactions t {where}";
        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, parameters);

        var offset = (page - 1) * pageSize;
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var dataSql = $@"SELECT t.TransactionId, t.TransactionDate, t.Name, t.Notes, t.Details, t.Category, t.SubCategory,
                                t.Amount, t.Portfolio, t.PropertyId, t.PropertyRaw, t.Unit, t.DataSource, t.Account, t.Owner
                         FROM Transactions t {where}
                         ORDER BY t.{orderCol} {orderDir}
                         OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var items = await conn.QueryAsync<Transaction>(dataSql, parameters);
        return (items, totalCount);
    }

    public async Task<int> CreateTransactionAsync(Transaction txn)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QuerySingleAsync<int>(
            @"INSERT INTO Transactions (TransactionDate, Name, Notes, Details, Category, SubCategory, Amount, Portfolio, PropertyId, PropertyRaw, Unit, DataSource, Account, Owner)
              OUTPUT INSERTED.TransactionId
              VALUES (@TransactionDate, @Name, @Notes, @Details, @Category, @SubCategory, @Amount, @Portfolio, @PropertyId, @PropertyRaw, @Unit, @DataSource, @Account, @Owner)",
            txn);
    }

    public async Task<bool> UpdateTransactionAsync(Transaction txn)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.ExecuteAsync(
            @"UPDATE Transactions SET TransactionDate=@TransactionDate, Name=@Name, Notes=@Notes, Details=@Details,
              Category=@Category, SubCategory=@SubCategory, Amount=@Amount, Portfolio=@Portfolio,
              PropertyId=@PropertyId, PropertyRaw=@PropertyRaw, Unit=@Unit, DataSource=@DataSource, Account=@Account, Owner=@Owner
              WHERE TransactionId=@TransactionId",
            txn);
        return rows > 0;
    }

    public async Task<bool> DeleteTransactionAsync(int transactionId)
    {
        using var conn = _factory.CreateConnection();
        var rows = await conn.ExecuteAsync("DELETE FROM Transactions WHERE TransactionId=@Id", new { Id = transactionId });
        return rows > 0;
    }

    public async Task<IEnumerable<string>> GetDistinctCategoriesAsync()
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryAsync<string>(
            "SELECT DISTINCT Category FROM Transactions WHERE Category IS NOT NULL ORDER BY Category");
    }
}
