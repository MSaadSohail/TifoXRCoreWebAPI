using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Item;

namespace GMS.TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface IItemRepository
    {
        Task<IReadOnlyList<ShopItemsListRow>> ListAllBySpaceAsync(int spaceId);
        Task<ItemCreatedEnvelope> AddAsync(int spaceId, CreateItemDto dto, string modifiedBy);
        Task<HashSet<string>> GetSupportedLocalesAsync(int spaceId);
        Task<bool> RegionalCurrencyExistsAsync(int regionalCurrencyId);
        Task UpdateAsync(int spaceId, int itemId, UpdateItemDto dto, string modifiedBy);
        Task DeleteAsync(int spaceId, int itemId, string modifiedBy);
    }

    
    public sealed class ItemCreatedEnvelope
    {
        public int ItemId { get; init; }
        public int ItemTypeId { get; init; }
        public string NameKey { get; init; } = default!;
        public string? DescriptionKey { get; init; }
        public bool IsAvailable { get; init; }
    }
}
