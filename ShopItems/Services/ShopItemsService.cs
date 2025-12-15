// <copyright file="ShopItemsService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>11/25/2025</date>
// <summary>Service implementation for shop-item listing.</summary>

using GMS.TifoXRCoreWebAPI.Middleware.Errors;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public sealed class ShopItemsService : IShopItemsService
    {
        private readonly IShopItemsRepository _repo;

        public ShopItemsService(IShopItemsRepository repo) => _repo = repo;

        public async Task<IReadOnlyList<ShopItemsListRow>> ListByShopAsync(int spaceId, int shopId)
        {
            // Controller is responsible for argument validation. Here we ensure 404 semantics.
            var exists = await _repo.ShopExistsInSpaceAsync(spaceId, shopId);
            if (!exists)
                throw new KeyNotFoundException("Shop not found for this space.");

            return await _repo.ListByShopAsync(spaceId, shopId);
        }

        public async Task<ShopItemsGroupedResponse> ListByShopGroupedAsync(int spaceId, int shopId)
        {
            // 404 semantics
            if (!await _repo.ShopExistsInSpaceAsync(spaceId, shopId))
                throw new KeyNotFoundException("Shop not found for this space.");

            // call the repo helper that groups
            return await _repo.ListByShopGroupedAsync(spaceId, shopId);
        }

        public async Task<ShopItemsListRow> AddAsync(int spaceId, int shopId, CreateShopItemLinkDto dto, string modifiedBy)
        {
            // Validate & insert mapping
            var created = await _repo.AddLinkAsync(spaceId, shopId, dto, modifiedBy);

            // Optional: attach discount after mapping is created
            if (dto.Discount is not null)
            {
                await _repo.UpsertItemDiscountAsync(dto.ItemId, dto.Discount.RegionId, dto.Discount.DiscountId, modifiedBy);
                created = await _repo.GetOneAsync(spaceId, shopId, dto.ItemId, dto.RegionalCurrencyId);
            }

            return created;
        }

        //public async Task<ShopItemsListRow> AddAsync(int spaceId, int shopId, CreateShopItemDtoV2 dto, string modifiedBy)
        //{
        //    // ---- basic null/shape checks ----
        //    if (dto is null)
        //        throw ErrorService.Exception(
        //            ErrorType.ArgumentNull, nameof(AddAsync),
        //            ErrorMessages.Validation.MissingParameter, paramName: nameof(dto),
        //            parameters: new { spaceId, shopId });

        //    if (dto.ItemTypeId <= 0)
        //        throw ErrorService.Exception(
        //            ErrorType.Argument, nameof(AddAsync),
        //            ErrorMessages.Validation.MissingParameter, paramName: nameof(dto.ItemTypeId),
        //            parameters: new { spaceId, shopId, dto.ItemTypeId });

        //    if (string.IsNullOrWhiteSpace(dto.NameKey))
        //        throw ErrorService.Exception(
        //            ErrorType.Argument, nameof(AddAsync),
        //            ErrorMessages.Validation.MissingParameter, paramName: nameof(dto.NameKey),
        //            parameters: new { spaceId, shopId });

        //    if (dto.RegionalCurrencyId <= 0)
        //        throw ErrorService.Exception(
        //            ErrorType.Argument, nameof(AddAsync),
        //            ErrorMessages.Validation.MissingParameter, paramName: nameof(dto.RegionalCurrencyId),
        //            parameters: new { spaceId, shopId, dto.RegionalCurrencyId });

        //    // Ensure the required subtype payload is present
        //    var subtypeProvided = dto.ItemTypeId switch
        //    {
        //        1 => dto.Merchandise is not null,
        //        2 => dto.Ticket is not null,
        //        3 => dto.Badge is not null,
        //        _ => false
        //    };
        //    if (!subtypeProvided)
        //        throw ErrorService.Exception(
        //            ErrorType.Argument, nameof(AddAsync),
        //            ErrorMessages.Validation.ValidationFailed,
        //            parameters: new { spaceId, shopId, dto.ItemTypeId },
        //            extra: "Required subtype payload is missing for the given item_type_id.");

        //    // ---- validate shop exists in space ----
        //    if (!await _repo.ShopExistsInSpaceAsync(spaceId, shopId))
        //        throw ErrorService.Exception(
        //            ErrorType.NotFound, nameof(AddAsync),
        //            ErrorMessages.Http.NotFound, parameters: new { spaceId, shopId });

        //    // ---- validate regional currency ----
        //    if (!await _repo.RegionalCurrencyExistsAsync(dto.RegionalCurrencyId))
        //        throw ErrorService.Exception(
        //            ErrorType.Argument, nameof(AddAsync),
        //            ErrorMessages.Validation.MissingParameter, paramName: nameof(dto.RegionalCurrencyId),
        //            parameters: new { dto.RegionalCurrencyId });

        //    // ---- validate locales against supported_languages(spaceId) ----
        //    var supported = await _repo.GetSupportedLocalesAsync(spaceId);

        //    if (dto.NameLocalizedPairs is not null)
        //    {
        //        // key must match the NameKey we’re creating on item
        //        if (!string.Equals(dto.NameLocalizedPairs.Key, dto.NameKey, StringComparison.Ordinal))
        //            throw ErrorService.Exception(
        //                ErrorType.Argument, nameof(AddAsync),
        //                ErrorMessages.Validation.ValidationFailed,
        //                parameters: new { expectedKey = dto.NameKey, providedKey = dto.NameLocalizedPairs.Key },
        //                extra: "name_localized_pairs.key must match item.name_key.");

        //        var badLocales = dto.NameLocalizedPairs.Values?
        //            .Select(v => v.LocaleId)
        //            .Where(loc => !string.IsNullOrWhiteSpace(loc) && !supported.Contains(loc))
        //            .Distinct(StringComparer.OrdinalIgnoreCase)
        //            .ToArray() ?? Array.Empty<string>();

        //        if (badLocales.Length > 0)
        //            throw ErrorService.Exception(
        //                ErrorType.InvalidOperation, nameof(AddAsync),
        //                ErrorMessages.Validation.ValidationFailed,
        //                parameters: new { spaceId, unsupported = badLocales },
        //                extra: "One or more locales are not supported by this space.");
        //    }

        //    if (!string.IsNullOrWhiteSpace(dto.DescriptionKey) && dto.DescriptionLocalizedPairs is not null)
        //    {
        //        if (!string.Equals(dto.DescriptionLocalizedPairs.Key, dto.DescriptionKey, StringComparison.Ordinal))
        //            throw ErrorService.Exception(
        //                ErrorType.Argument, nameof(AddAsync),
        //                ErrorMessages.Validation.ValidationFailed,
        //                parameters: new { expectedKey = dto.DescriptionKey, providedKey = dto.DescriptionLocalizedPairs.Key },
        //                extra: "description_localized_pairs.key must match item.description_key.");

        //        var badLocales = dto.DescriptionLocalizedPairs.Values?
        //            .Select(v => v.LocaleId)
        //            .Where(loc => !string.IsNullOrWhiteSpace(loc) && !supported.Contains(loc))
        //            .Distinct(StringComparer.OrdinalIgnoreCase)
        //            .ToArray() ?? Array.Empty<string>();

        //        if (badLocales.Length > 0)
        //            throw ErrorService.Exception(
        //                ErrorType.InvalidOperation, nameof(AddAsync),
        //                ErrorMessages.Validation.ValidationFailed,
        //                parameters: new { spaceId, unsupported = badLocales },
        //                extra: "One or more locales are not supported by this space.");
        //    }

        //    try
        //    {
        //        // repo creates: item → subtype → item_prices → shop_items (single TX)
        //        return await _repo.AddAsync(spaceId, shopId, dto, modifiedBy);
        //    }
        //    catch (InvalidOperationException ex) when (ex.Message.StartsWith("Duplicate mapping", StringComparison.Ordinal))
        //    {
        //        // uniqueness on (shop_id, item_id, regional_currency_id)
        //        throw ErrorService.Exception(
        //            ErrorType.Conflict, nameof(AddAsync),
        //            ErrorMessages.Http.Conflict,
        //            parameters: new
        //            {
        //                spaceId,
        //                shopId,
        //                regionalCurrencyId = dto.RegionalCurrencyId,
        //                nameKey = dto.NameKey
        //            },
        //            extra: "A mapping for (shop_id, item_id, regional_currency_id) already exists.");
        //    }
        //}

        public async Task<ShopItemsListRow> UpdateAsync(int spaceId, int shopId, int itemId, UpdateShopItemDto dto, string modifiedBy)
        {
            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull, nameof(UpdateAsync),
                    ErrorMessages.Validation.MissingParameter, paramName: nameof(dto),
                    parameters: new { spaceId, shopId, itemId });

            if (dto.RegionalCurrencyId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument, nameof(UpdateAsync),
                    ErrorMessages.Validation.PositiveIntRequired, paramName: nameof(dto.RegionalCurrencyId),
                    parameters: new { spaceId, shopId, itemId });

            // 1) mapping must exist and belong to space
            var exists = await _repo.MappingExistsInSpaceAsync(spaceId, shopId, itemId, dto.RegionalCurrencyId);
            if (!exists)
                throw ErrorService.Exception(
                    ErrorType.NotFound, nameof(UpdateAsync),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, shopId, itemId, dto.RegionalCurrencyId });

            // 2) load item snapshot for key checks
            var snap = await _repo.GetItemSnapshotAsync(itemId);
            if (snap is null)
                throw ErrorService.Exception(
                    ErrorType.NotFound, nameof(UpdateAsync),
                    ErrorMessages.Http.NotFound,
                    parameters: new { itemId });

            // 3–4) i18n validations
            var supported = await _repo.GetSupportedLocalesAsync(spaceId);

            if (dto.NameLocalizedPairs is not null)
            {
                if (!string.Equals(dto.NameLocalizedPairs.Key, snap.NameKey, StringComparison.Ordinal))
                    throw ErrorService.Exception(
                        ErrorType.Argument, nameof(UpdateAsync),
                        ErrorMessages.Validation.ValidationFailed,
                        parameters: new { expectedKey = snap.NameKey, providedKey = dto.NameLocalizedPairs.Key },
                        extra: "nameLocalizedPairs.key must match item.name_key.");

                var bad = dto.NameLocalizedPairs.Values?
                    .Select(v => v.LocaleId)
                    .Where(loc => !supported.Contains(loc ?? string.Empty))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray() ?? Array.Empty<string>();

                if (bad.Length > 0)
                    throw ErrorService.Exception(
                        ErrorType.Argument, nameof(UpdateAsync),
                        ErrorMessages.Validation.ValidationFailed,
                        parameters: new { spaceId, badLocales = bad },
                        extra: "Unsupported locales for this space.");
            }

            if (dto.DescriptionLocalizedPairs is not null)
            {
                if (string.IsNullOrWhiteSpace(snap.DescriptionKey))
                    throw ErrorService.Exception(
                        ErrorType.Argument, nameof(UpdateAsync),
                        ErrorMessages.Validation.ValidationFailed,
                        parameters: new { itemId },
                        extra: "Item has no description_key; cannot upsert description localizations.");

                if (!string.Equals(dto.DescriptionLocalizedPairs.Key, snap.DescriptionKey, StringComparison.Ordinal))
                    throw ErrorService.Exception(
                        ErrorType.Argument, nameof(UpdateAsync),
                        ErrorMessages.Validation.ValidationFailed,
                        parameters: new { expectedKey = snap.DescriptionKey, providedKey = dto.DescriptionLocalizedPairs.Key },
                        extra: "descriptionLocalizedPairs.key must match item.description_key.");

                var bad = dto.DescriptionLocalizedPairs.Values?
                    .Select(v => v.LocaleId)
                    .Where(loc => !supported.Contains(loc ?? string.Empty))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray() ?? Array.Empty<string>();

                if (bad.Length > 0)
                    throw ErrorService.Exception(
                        ErrorType.Argument, nameof(UpdateAsync),
                        ErrorMessages.Validation.ValidationFailed,
                        parameters: new { spaceId, badLocales = bad },
                        extra: "Unsupported locales for this space.");
            }

            // 5–6) persist + return updated envelope
            return await _repo.UpdateAsync(spaceId, shopId, itemId, dto, modifiedBy);
        }

        public async Task DeleteAsync(int spaceId, int shopId, int itemId)
        {
            var snap = await _repo.GetItemSnapshotAsync(itemId);
            if (snap is null || snap.SpaceId != spaceId)
                throw ErrorService.Exception(
                    ErrorType.NotFound, nameof(DeleteAsync),
                    ErrorMessages.Http.NotFound,
                    parameters: new { itemId, spaceId });

            var ok = await _repo.DeleteShopItemAndDiscountsAsync(spaceId, shopId, itemId);
            if (!ok)
                throw ErrorService.Exception(
                    ErrorType.NotFound, nameof(DeleteAsync),
                    ErrorMessages.Http.NotFound,
                    parameters: new { shopId, itemId });
        }



        public async Task DeleteMappingAsync(int spaceId, int shopId, int itemId, int regionalCurrencyId, string modifiedBy)
        {
            var exists = await _repo.MappingExistsInSpaceAsync(spaceId, shopId, itemId, regionalCurrencyId);
            if (!exists)
                throw ErrorService.Exception(
                    ErrorType.NotFound, nameof(DeleteMappingAsync),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, shopId, itemId, regionalCurrencyId });

            try
            {
                await _repo.DeleteMappingAsync(spaceId, shopId, itemId, regionalCurrencyId, modifiedBy);
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("constraint", StringComparison.OrdinalIgnoreCase))
            {
                throw ErrorService.Exception(
                    ErrorType.Conflict, nameof(DeleteMappingAsync),
                    ErrorMessages.Http.Conflict,
                    parameters: new { spaceId, shopId, itemId, regionalCurrencyId },
                    extra: "Delete blocked by foreign key references.");
            }
        }
    }

}
