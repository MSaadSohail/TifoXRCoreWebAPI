using GMS.TifoXRCoreWebAPI.Middleware.Errors;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Models.Item;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;

namespace GMS.TifoXRCoreWebAPI.Services
{

    public sealed class ItemService : IItemService
    {
        private readonly IItemRepository _repo;
        public ItemService(IItemRepository repo) => _repo = repo;

        public async Task<ItemCreatedEnvelope> AddAsync(int spaceId, CreateItemDto dto, string modifiedBy)
        {
            if (dto is null)
                throw ErrorService.Exception(ErrorType.ArgumentNull, nameof(AddAsync),
                    ErrorMessages.Validation.MissingParameter, paramName: nameof(dto), parameters: new { spaceId });

            // Validate subtype presence
            var subtypeCount =
                (dto.ItemTypeId == 1 && dto.Merchandise != null ? 1 : 0) +
                (dto.ItemTypeId == 2 && dto.Ticket != null ? 1 : 0) +
                (dto.ItemTypeId == 3 && dto.Badge != null ? 1 : 0);

            if (subtypeCount != 1)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(AddAsync),
                    ErrorMessages.Validation.ValidationFailed,
                    extra: "Exactly one subtype payload must match ItemTypeId."
                );

            // Optional one-price validation
            if (dto.RegionalCurrencyId.HasValue != dto.BaseCost.HasValue)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(AddAsync),
                    ErrorMessages.Validation.ValidationFailed,
                    extra: "Both RegionalCurrencyId and BaseCost must be supplied together or omitted."
                );

            if (dto.RegionalCurrencyId is int rcId && !await _repo.RegionalCurrencyExistsAsync(rcId))
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(AddAsync),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto.RegionalCurrencyId),
                    parameters: new { dto.RegionalCurrencyId });

            // Locale gating
            var supported = await _repo.GetSupportedLocalesAsync(spaceId);
            ValidatePairs(dto.NameKey, dto.NameLocalizedPairs, supported, "name_localized_pairs");
            if (!string.IsNullOrWhiteSpace(dto.DescriptionKey))
                ValidatePairs(dto.DescriptionKey!, dto.DescriptionLocalizedPairs, supported, "description_localized_pairs");

            return await _repo.AddAsync(spaceId, dto, modifiedBy);
        }

        private static void ValidatePairs(string expectedKey, LocalizedPairs? pairs, HashSet<string> supported, string field)
        {
            if (pairs is null) return;

            if (!string.Equals(pairs.Key, expectedKey, StringComparison.Ordinal))
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(AddAsync),
                    ErrorMessages.Validation.ValidationFailed,
                    extra: $"{field}.key must match the item key."
                );

            var bad = pairs.Values?
                .Select(v => v.LocaleId)
                .Where(loc => !string.IsNullOrWhiteSpace(loc) && !supported.Contains(loc))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? Array.Empty<string>();

            if (bad.Length > 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(AddAsync),
                    ErrorMessages.Validation.ValidationFailed,
                    parameters: new { unsupported = bad },
                    extra: "One or more locales are not supported by this space."
                );
        }

        public Task<IReadOnlyList<ShopItemsListRow>> ListAllAsync(int spaceId)
            => _repo.ListAllBySpaceAsync(spaceId);


        public async Task UpdateAsync(int spaceId, int itemId, UpdateItemDto dto, string modifiedBy)
        {
            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull, nameof(UpdateAsync),
                    ErrorMessages.Validation.MissingParameter,
                    paramName: nameof(dto),
                    parameters: new { spaceId, itemId });

            // Optional: validate locales upfront like you do elsewhere
            // (Skip here for brevity; repository will upsert what you pass.)

            try
            {
                await _repo.UpdateAsync(spaceId, itemId, dto, modifiedBy);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                throw ErrorService.Exception(
                    ErrorType.NotFound, nameof(UpdateAsync),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, itemId });
            }
        }

        public Task DeleteAsync(int spaceId, int itemId, string modifiedBy)
        => _repo.DeleteAsync(spaceId, itemId, modifiedBy);
    }


}
