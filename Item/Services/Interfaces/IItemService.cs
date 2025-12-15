using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Item;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public interface IItemService
    {
        Task<ItemCreatedEnvelope> AddAsync(int spaceId, CreateItemDto dto, string modifiedBy);
        Task<IReadOnlyList<ShopItemsListRow>> ListAllAsync(int spaceId);

        Task UpdateAsync(int spaceId, int itemId, UpdateItemDto dto, string modifiedBy);

        Task DeleteAsync(int spaceId, int itemId, string modifiedBy);
    }
}