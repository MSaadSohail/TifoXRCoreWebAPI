using TifoXRCoreWebAPI.Models;

namespace TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface ITeleportRepository
    {
        Task<List<TeleportTableData>> GetTeleportTablesBySpaceAsync(int spaceId);
        Task<TeleportTableData> UpdateTeleportTableAsync(int spaceId, int tableId, TeleportTableUpdateDto dto);
    }
}
