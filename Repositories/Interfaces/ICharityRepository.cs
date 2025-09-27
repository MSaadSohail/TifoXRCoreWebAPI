// Repositories/Interfaces/ICharityRepository.cs
using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface ICharityRepository
    {
        Task<CharityData> CreateCharityAsync(CharityCreateDto dto);
        Task<CharityData?> GetCharityByIdAsync(int id, int spaceId);
        Task<List<CharityData>> GetCharitiesBySpaceAsync(int spaceId);

        Task<CharityData> UpdateCharityAsync(int spaceId, int id, CharityUpdateDto dto);

        Task<bool> DeleteCharityAsync(int spaceId, int id);

    }
}
