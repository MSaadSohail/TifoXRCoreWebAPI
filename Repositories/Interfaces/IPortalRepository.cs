using TifoXRCoreWebAPI.Models;

namespace TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface IPortalRepository
    {
        Task<List<PortalData>> GetPortalsBySpaceAsync(int spaceId);
        Task<List<PortalData>> GetPortalsByBoothAsync(int spaceId, int boothId);
        Task<PortalData?> GetPortalByIdAsync(int spaceId, int portalId);
        Task<PortalData> CreatePortalAsync(int spaceId, PortalCreateDto portalDto);
        Task<PortalData?> UpdatePortalAsync(int spaceId, int portalId, PortalUpdateDto portalDto);
        Task<PortalData?> UpdatePortalAsync(int spaceId, int boothId, int portalId, PortalUpdateDto dto)
        Task<bool> DeletePortalAsync(int spaceId, int portalId);
        Task<bool> DeletePortalAsync(int spaceId, int boothId, int portalId);
    }
}
