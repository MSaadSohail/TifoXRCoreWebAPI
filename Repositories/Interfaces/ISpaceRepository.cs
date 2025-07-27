using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface ISpaceRepository
    {
        Task<SpaceData> GetSpaceByIdAsync(int id);
        Task<SpaceData> CreateSpaceAsync(Space spaceDto);
        Task<SpaceData> UpdateSpaceAsync(int id, Space spaceDto);

    }
}
