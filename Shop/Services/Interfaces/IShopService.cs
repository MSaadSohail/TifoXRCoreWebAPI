// <copyright file="IShopService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>11/25/2025</date>
// <summary>Service contract for Shop</summary>

using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public interface IShopService
    {
        Task<IReadOnlyList<ShopDto>> ListBySpaceAsync(int spaceId);
        Task<ShopDto?> GetByIdAsync(int spaceId, int shopId);

        Task<ShopDto> CreateAsync(int spaceId, CreateShopDto dto, string modifiedBy = "system");

        /// <summary>Update core shop fields; optional i18n upsert if provided. Returns updated row.</summary>
        Task<ShopDto> UpdateAsync(int spaceId, int shopId, UpdateShopDto dto, string modifiedBy = "system");

        Task DeleteAsync(int spaceId, int shopId, string modifiedBy);
    }
}
