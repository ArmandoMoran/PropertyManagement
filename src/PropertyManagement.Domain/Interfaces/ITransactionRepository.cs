using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Domain.Interfaces;

public interface ITransactionRepository
{
    Task<IEnumerable<Transaction>> GetTransactionsByPropertyAndYearAsync(int propertyId, int year);
    Task<IEnumerable<Transaction>> GetAllTransactionsByYearAsync(int year);
    Task<IEnumerable<int>> GetDistinctYearsAsync();
    Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedTransactionsAsync(
        int? propertyId, int page, int pageSize, string? category, string? search,
        DateTime? startDate, DateTime? endDate, string sortBy, bool sortDesc);
    Task<int> CreateTransactionAsync(Transaction txn);
    Task<bool> UpdateTransactionAsync(Transaction txn);
    Task<bool> DeleteTransactionAsync(int transactionId);
    Task<IEnumerable<string>> GetDistinctCategoriesAsync();
}
