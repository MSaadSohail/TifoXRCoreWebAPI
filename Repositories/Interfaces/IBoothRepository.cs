using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface IBoothRepository
    {
        Task<List<BoothModel>> GetAllBoothsBySpaceAsync(int spaceId);
        Task<BoothModel> UpdateBoothAsync(int spaceId, int boothId, BoothUpdateDto dto);
        Task<BoothModel> CreateBoothAsync(int spaceId, BoothCreateDto boothDto);
        Task<bool> DeleteBoothCascadeAsync(int spaceId, int boothId);
    }
}
