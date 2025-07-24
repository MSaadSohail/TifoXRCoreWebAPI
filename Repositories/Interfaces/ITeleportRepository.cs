using TifoXRCoreWebAPI.Models;

namespace TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface ITeleportRepository
    {
        Task<TeleportTableData> GetTeleportTableBySpaceAsync(int spaceId);
        Task<TeleportTableData> UpdateTeleportTableAsync(int spaceId, int tableId, TeleportTableUpdateDto dto);
    }
}
