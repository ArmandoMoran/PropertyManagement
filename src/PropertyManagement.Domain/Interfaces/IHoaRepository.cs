using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Domain.Interfaces;

public interface IHoaRepository
{
    Task<HoaInfo?> GetHoaByPropertyIdAsync(int propertyId);
    Task<IEnumerable<HoaInfo>> GetAllHoaByPropertyIdAsync(int propertyId);
    Task<int> CreateHoaAsync(HoaInfo hoa);
    Task<bool> UpdateHoaAsync(HoaInfo hoa);
    Task<bool> DeleteHoaAsync(int hoaId);
}
