// <copyright file="ShopService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Your Name</author>
// <date>11/25/2025</date>
// <summary>Service implementation for Shop</summary>

using GMS.TifoXRCoreWebAPI.Middleware.Errors;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public sealed class ShopService : IShopService
    {
        private readonly IShopRepository _repo;
        public ShopService(IShopRepository repo) => _repo = repo;

        public Task<IReadOnlyList<ShopDto>> ListBySpaceAsync(int spaceId)
            => _repo.ListBySpaceAsync(spaceId);

        public Task<ShopDto?> GetByIdAsync(int spaceId, int shopId)
            => _repo.GetByIdAsync(spaceId, shopId);

        public async Task<ShopDto> CreateAsync(int spaceId, CreateShopDto dto, string modifiedBy = "system")
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.NameKey))
                throw new ArgumentException("name_key is required.", nameof(dto.NameKey));

            // Validate references
            if (!await _repo.SpaceExistsAsync(spaceId))
                throw new InvalidOperationException($"Space {spaceId} does not exist.");
            if (!await _repo.PlatformTypeExistsAsync(dto.PlatformTypeId))
                throw new InvalidOperationException($"PlatformType {dto.PlatformTypeId} does not exist.");
            if (dto.EntityId is int eid && !await _repo.EntityExistsAsync(eid))
                throw new InvalidOperationException($"Entity {eid} does not exist.");

            // Localizations (optional)
            if (dto.LocalizedPairs is not null)
            {
                if (!string.Equals(dto.LocalizedPairs.Key, dto.NameKey, StringComparison.Ordinal))
                    throw new InvalidOperationException("localized_pairs.key must match name_key.");

                if (dto.LocalizedPairs.Values is { Count: > 0 })
                {
                    var supported = await _repo.GetSupportedLocalesAsync(spaceId);
                    var provided = dto.LocalizedPairs.Values.Select(v => v.LocaleId)
                                      .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var unsupported = provided.Where(p => !supported.Contains(p)).ToArray();
                    if (unsupported.Length > 0)
                        throw new UnsupportedLocalesException(unsupported);
                }
            }

            return await _repo.CreateAsync(spaceId, dto, modifiedBy);
        }
        public async Task<ShopDto> UpdateAsync(int spaceId, int shopId, UpdateShopDto dto, string modifiedBy = "system")
        {
            if (!await _repo.ExistsInSpaceAsync(spaceId, shopId))
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(UpdateAsync),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, shopId });

            // get current name_key
            var oldKey = await _repo.GetCurrentNameKeyAsync(spaceId, shopId)
                        ?? throw ErrorService.Exception(
                            ErrorType.NotFound,
                            nameof(UpdateAsync),
                            ErrorMessages.Http.NotFound,
                            parameters: new { spaceId, shopId });

            var finalKey = dto.NameKey ?? oldKey;

            // if renaming, enforce uniqueness within the space
            if (!string.Equals(finalKey, oldKey, StringComparison.Ordinal) &&
                await _repo.IsNameKeyDuplicateAsync(spaceId, finalKey, shopId))
            {
                throw ErrorService.Exception(
                    ErrorType.Conflict,
                    nameof(UpdateAsync),
                    ErrorMessages.Http.Conflict,
                    extra: "Duplicate name key",
                    parameters: new { spaceId, shopId, name_key = finalKey });
            }

            // Validate localized pairs (if provided)
            if (dto.LocalizedPairs is not null)
            {
                if (!string.Equals(dto.LocalizedPairs.Key, finalKey, StringComparison.Ordinal))
                {
                    throw ErrorService.Exception(
                        ErrorType.Argument,
                        nameof(UpdateAsync),
                        ErrorMessages.Validation.MissingParameter,
                        paramName: "localizedPairs.key",
                        extra: "localizedPairs.key must equal the final name_key",
                        parameters: new { spaceId, shopId, finalKey, dto.LocalizedPairs.Key });
                }

                var supported = await _repo.GetSupportedLocalesAsync(spaceId);
                var supportedSet = supported.ToHashSet(StringComparer.OrdinalIgnoreCase);

                var provided = dto.LocalizedPairs.Values?.Select(v => v.LocaleId).ToList() ?? new();
                var unsupported = provided.Where(p => !supportedSet.Contains(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                if (unsupported.Count > 0)
                {
                    // 422 Unprocessable Entity
                    throw ErrorService.Exception(
                        ErrorType.InvalidOperation,
                        nameof(UpdateAsync),
                        ErrorMessages.Http.UnsupportedMediaType,
                        extra: "Unsupported locales provided.",
                        parameters: new { spaceId, shopId, unsupported });
                }
            }

            // persist partial update
            await _repo.UpdatePartialAsync(
                spaceId: spaceId,
                shopId: shopId,
                platformTypeId: dto.PlatformTypeId,
                hasEntityId: dto.HasEntityId,
                entityId: dto.HasEntityId ? dto.EntityId : null,
                nameKey: dto.NameKey,
                modifiedBy: modifiedBy);

            // i18n upsert if provided
            if (dto.LocalizedPairs is not null && dto.LocalizedPairs.Values is { Count: > 0 })
            {
                await _repo.UpsertI18nAsync(spaceId, finalKey, dto.LocalizedPairs.Values, modifiedBy);
            }

            // return updated row
            var updated = await _repo.GetByIdAsync(spaceId, shopId)
                          ?? throw ErrorService.Exception(
                              ErrorType.NotFound,
                              nameof(UpdateAsync),
                              ErrorMessages.Http.NotFound,
                              parameters: new { spaceId, shopId });

            return updated;
        }

        public async Task DeleteAsync(int spaceId, int shopId, string modifiedBy)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(DeleteAsync),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, shopId });

            if (shopId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(DeleteAsync),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(shopId),
                    parameters: new { spaceId, shopId });

            // Cheap existence check (repo ensures space guard inside delete too)
            var existing = await _repo.GetByIdAsync(spaceId, shopId);
            if (existing is null)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(DeleteAsync),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, shopId });

            try
            {
                var affected = await _repo.HardDeleteAsync(spaceId, shopId, modifiedBy);
                if (affected == 0)
                    throw new InvalidOperationException("NoRowsAffected");
            }
            catch (InvalidOperationException ex) when (
                ex.Message.StartsWith("FK_CONSTRAINT", StringComparison.Ordinal))
            {
                throw ErrorService.Exception(
                    ErrorType.Conflict,
                    nameof(DeleteAsync),
                    ErrorMessages.Http.Conflict,
                    parameters: new { spaceId, shopId },
                    extra: "Hard delete blocked by foreign key references. Remove dependents first.");
            }
            catch (InvalidOperationException ex) when (
                ex.Message.StartsWith("TX_ALREADY_COMPLETED", StringComparison.Ordinal) ||
                ex.Message.StartsWith("Already committed or rolled back", StringComparison.Ordinal))
            {
                throw ErrorService.Exception(
                    ErrorType.Conflict,
                    nameof(DeleteAsync),
                    ErrorMessages.Http.Conflict,
                    parameters: new { spaceId, shopId },
                    extra: ex.Message);
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Equals("NoRowsAffected", StringComparison.Ordinal))
            {
                throw ErrorService.Exception(
                    ErrorType.InvalidOperation,
                    nameof(DeleteAsync),
                    ErrorMessages.Http.InternalServerError,
                    parameters: new { spaceId, shopId },
                    extra: "No rows affected despite existence check.");
            }
        }
    }

    // Simple domain exception for 422
    public sealed class UnsupportedLocalesException : Exception
    {
        public IReadOnlyList<string> Locales { get; }
        public UnsupportedLocalesException(IEnumerable<string> locales)
            : base("One or more locales are not supported for this space.")
        {
            Locales = locales.ToArray();
        }
    }


}
