// <copyright file="IShopItemsService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>11/25/2025</date>
// <summary>Service interface for shop-item listing.</summary>

using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public interface IShopItemsService
    {
        Task<IReadOnlyList<ShopItemsListRow>> ListByShopAsync(int spaceId, int shopId);

        Task<ShopItemsGroupedResponse> ListByShopGroupedAsync(int spaceId, int shopId);

        Task<ShopItemsListRow> AddAsync(int spaceId, int shopId, CreateShopItemLinkDto dto, string modifiedBy);

        Task<ShopItemsListRow> UpdateAsync(int spaceId, int shopId, int itemId, UpdateShopItemDto dto, string modifiedBy);

        Task DeleteAsync(int spaceId, int shopId, int itemId);

        Task DeleteMappingAsync(int spaceId, int shopId, int itemId, int regionalCurrencyId, string modifiedBy);

    }
}
