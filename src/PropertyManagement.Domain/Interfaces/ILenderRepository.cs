using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Domain.Interfaces;

public interface ILenderRepository
{
    Task<Lender?> GetLenderByPropertyIdAsync(int propertyId);
    Task<IEnumerable<Lender>> GetAllLendersByPropertyIdAsync(int propertyId);
    Task<int> CreateLenderAsync(Lender lender);
    Task<bool> UpdateLenderAsync(Lender lender);
    Task<bool> DeleteLenderAsync(int lenderId);
    Task<IEnumerable<PrincipalBalanceHistory>> GetPrincipalBalanceHistoryAsync(int propertyId);
    Task<int> CreatePrincipalBalanceAsync(PrincipalBalanceHistory balance);
}
