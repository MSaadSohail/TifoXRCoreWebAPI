// <copyright file="IShopRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>11/25/2025</date>
// <summary>Repository contract for Shop</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public interface IShopRepository
    {
        Task<IReadOnlyList<ShopDto>> ListBySpaceAsync(int spaceId);
        Task<ShopDto?> GetByIdAsync(int spaceId, int shopId);

        Task<ShopDto> CreateAsync(int spaceId, CreateShopDto dto, string modifiedBy);
        Task<bool> SpaceExistsAsync(int spaceId);
        Task<bool> PlatformTypeExistsAsync(int platformTypeId);
        Task<bool> EntityExistsAsync(int entityId);
        Task<HashSet<string>> GetSupportedLocalesAsync(int spaceId);
        Task InsertI18nAsync(int spaceId, string key, IEnumerable<LocalizedValue> values, string modifiedBy);
        Task<bool> ExistsInSpaceAsync(int spaceId, int shopId);
        Task<string?> GetCurrentNameKeyAsync(int spaceId, int shopId);
        Task<bool> IsNameKeyDuplicateAsync(int spaceId, string nameKey, int excludeShopId);

        Task UpdatePartialAsync(int spaceId, int shopId, int? platformTypeId, bool hasEntityId, int? entityId, string? nameKey, string modifiedBy);

        /// <summary>Upsert i18n values for the given key and locales (per value: UPDATE-first, then INSERT-if-missing).</summary>
        Task UpsertI18nAsync(int spaceId, string key, IEnumerable<LocalizedValue> values, string modifiedBy);

        /// <summary>
        /// Hard-deletes a shop (and its shop_items) within the given space.
        /// Returns the number of deleted shop rows (0 if none).
        /// </summary>
        Task<int> HardDeleteAsync(int spaceId, int shopId, string modifiedBy);
    }
}
