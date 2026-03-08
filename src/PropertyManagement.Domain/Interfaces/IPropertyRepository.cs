using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Domain.Interfaces;

public interface IPropertyRepository
{
    Task<IEnumerable<Property>> GetAllPropertiesAsync();
    Task<Property?> GetPropertyByIdAsync(int propertyId);
    Task<IEnumerable<Property>> SearchPropertiesAsync(string searchTerm);
    Task<int> CreatePropertyAsync(Property property);
    Task<bool> UpdatePropertyAsync(Property property);
    Task<bool> DeletePropertyAsync(int propertyId);
    Task<IEnumerable<PropertyHistory>> GetPropertyHistoryAsync(int propertyId);
}
