using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Domain.Interfaces;

public interface IInsuranceRepository
{
    Task<Insurance?> GetInsuranceByPropertyIdAsync(int propertyId);
    Task<InsurancePremium?> GetPremiumByPropertyAndYearAsync(int propertyId, int year);
    Task<IEnumerable<InsurancePremium>> GetAllPremiumsByPropertyAsync(int propertyId);
    Task<int> CreateInsuranceAsync(Insurance ins);
    Task<bool> UpdateInsuranceAsync(Insurance ins);
    Task<int> CreatePremiumAsync(InsurancePremium prem);
}
