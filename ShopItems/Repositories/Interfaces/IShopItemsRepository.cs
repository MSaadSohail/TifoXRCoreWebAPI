// <copyright file="IShopItemsRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>11/25/2025</date>
// <summary>Repository interface for shop-item listing.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public interface IShopItemsRepository
    {
        Task<bool> ShopExistsInSpaceAsync(int spaceId, int shopId);
        Task<IReadOnlyList<ShopItemsListRow>> ListByShopAsync(int spaceId, int shopId);

        //Task<ShopItemsListRow> AddAsync(int spaceId, int shopId, CreateShopItemDtoV2 dto, string modifiedBy);
        Task<ItemNameDescSnapshot?> GetItemSnapshotAsync(int itemId);
        Task<bool> RegionalCurrencyExistsAsync(int regionalCurrencyId);
        Task<HashSet<string>> GetSupportedLocalesAsync(int spaceId);

        Task<int> UpsertI18nAsync(int spaceId, string key, IEnumerable<LocalizedValue> pairs, string modifiedBy);
        Task<bool> DiscountExistsAsync(int discountId);

        
        Task UpsertItemDiscountAsync(int itemId, int? regionId, int discountId, string modifiedBy);

        Task<bool> MappingExistsInSpaceAsync(int spaceId, int shopId, int itemId, int regionalCurrencyId);
        Task<ShopItemsListRow> UpdateAsync(int spaceId, int shopId, int itemId, UpdateShopItemDto dto, string modifiedBy);

        Task DeleteMappingAsync(int spaceId, int shopId, int itemId, int regionalCurrencyId, string modifiedBy);

        Task<ShopItemsListRow> AddLinkAsync(int spaceId, int shopId, CreateShopItemLinkDto dto, string modifiedBy);
        Task<ShopItemsListRow> GetOneAsync(int spaceId, int shopId, long itemId, int regionalCurrencyId);
        Task UpsertItemDiscountAsync(long itemId, int? regionId, int discountId, string modifiedBy);

        Task<bool> DeleteShopItemAndDiscountsAsync(int spaceId, int shopId, int itemId);

        Task<ShopItemsGroupedResponse> ListByShopGroupedAsync(int spaceId, int shopId);


    }

}