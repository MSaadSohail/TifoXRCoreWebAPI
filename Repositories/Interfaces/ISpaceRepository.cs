using TifoXRCoreWebAPI.Models;

namespace TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface ISpaceRepository
    {
        Task<SpaceData> GetSpaceByIdAsync(int id);
        Task<SpaceData> CreateSpaceAsync(Space spaceDto);
    }
}
