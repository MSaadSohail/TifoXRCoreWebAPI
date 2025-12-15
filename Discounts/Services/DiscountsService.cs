// <copyright file="DiscountService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>11/26/2025</date>
// <summary>SQL statements for discount definitions listing.</summary>

using GMS.TifoXRCoreWebAPI.Middleware.Errors;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public sealed class DiscountsService : IDiscountsService
    {
        private readonly IDiscountsRepository _repo;
        public DiscountsService(IDiscountsRepository repo) => _repo = repo;

        public async Task<IReadOnlyList<DiscountDto>> ListActiveBySpaceAsync(int spaceId)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument, nameof(ListActiveBySpaceAsync),
                    ErrorMessages.Validation.PositiveIntRequired, paramName: nameof(spaceId),
                    parameters: new { spaceId });

            return await _repo.ListActiveBySpaceAsync(spaceId);
        }

        public async Task<DiscountDto> CreateAsync(int spaceId, CreateDiscountDto dto, string modifiedBy)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument, nameof(CreateAsync),
                    ErrorMessages.Validation.PositiveIntRequired, paramName: nameof(spaceId),
                    parameters: new { spaceId });

            if (dto is null)
                throw ErrorService.Exception(
                    ErrorType.ArgumentNull, nameof(CreateAsync),
                    ErrorMessages.Validation.MissingParameter, paramName: nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.NameKey))
                throw ErrorService.Exception(
                    ErrorType.Argument, nameof(CreateAsync),
                    ErrorMessages.Validation.MissingParameter, paramName: nameof(dto.NameKey));

            // discount_type exists?
            if (!await _repo.DiscountTypeExistsAsync(dto.DiscountTypeId))
                throw ErrorService.Exception(
                    ErrorType.Argument, nameof(CreateAsync),
                    ErrorMessages.Validation.InvalidFormat,
                    paramName: nameof(dto.DiscountTypeId),
                    parameters: new { dto.DiscountTypeId },
                    extra: "Unknown discount_type_id.");

            // i18n validation (optional)
            if (dto.LocalizedPairs is not null)
            {
                if (!string.Equals(dto.LocalizedPairs.Key, dto.NameKey, StringComparison.Ordinal))
                    throw ErrorService.Exception(
                        ErrorType.Argument, nameof(CreateAsync),
                        ErrorMessages.Validation.RouteBodyMismatch,
                        paramName: nameof(dto.LocalizedPairs.Key),
                        extra: "localized_pairs.key must equal name_key.",
                        parameters: new { dto.NameKey, dto.LocalizedPairs.Key });

                var supported = await _repo.GetSupportedLocalesAsync(spaceId);
                var submitted = dto.LocalizedPairs.Values?.Select(v => v.LocaleId ?? "")
                                 .Where(s => !string.IsNullOrWhiteSpace(s))
                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                 .ToArray() ?? Array.Empty<string>();

                var invalid = submitted.Where(loc => !supported.Contains(loc)).ToArray();
                if (invalid.Length > 0)
                    throw ErrorService.Exception(
                        ErrorType.InvalidOperation, nameof(CreateAsync),
                        ErrorMessages.Validation.ValidationFailed,
                        extra: $"Unsupported locales: {string.Join(",", invalid)}",
                        parameters: new { spaceId });
            }

            return await _repo.CreateAsync(spaceId, dto, modifiedBy);
        }

        public async Task<DiscountDto> UpdateAsync(int spaceId, int discountId, UpdateDiscountDto dto, string modifiedBy)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument, nameof(UpdateAsync),
                    ErrorMessages.Validation.PositiveIntRequired, paramName: nameof(spaceId),
                    parameters: new { spaceId, discountId });

            if (discountId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument, nameof(UpdateAsync),
                    ErrorMessages.Validation.PositiveIntRequired, paramName: nameof(discountId),
                    parameters: new { spaceId, discountId });

            // If LocalizedPairs provided ⇒ spaceId must be provided in body (or we can use route spaceId)
            // We'll use the route spaceId; validate that LocalizedPairs.Key matches final NameKey.
            if (dto.LocalizedPairs is not null)
            {
                // final name key = dto.NameKey (if changing) else current one → we check in repo after load,
                // but we can do a fast pre-check if NameKey present:
                if (dto.NameKey is not null &&
                    !string.Equals(dto.LocalizedPairs.Key, dto.NameKey, StringComparison.Ordinal))
                {
                    throw ErrorService.Exception(
                        ErrorType.Argument, nameof(UpdateAsync),
                        ErrorMessages.Validation.RouteBodyMismatch,
                        paramName: nameof(dto.LocalizedPairs.Key),
                        extra: "localized_pairs.key must equal final name_key.",
                        parameters: new { dto.NameKey, dto.LocalizedPairs.Key });
                }

                var supported = await _repo.GetSupportedLocalesAsync(spaceId);
                var invalid = (dto.LocalizedPairs.Values ?? new()).Select(v => v.LocaleId ?? "")
                               .Where(id => !string.IsNullOrWhiteSpace(id) && !supported.Contains(id))
                               .Distinct(StringComparer.OrdinalIgnoreCase)
                               .ToArray();
                if (invalid.Length > 0)
                    throw ErrorService.Exception(
                        ErrorType.InvalidOperation, nameof(UpdateAsync),
                        ErrorMessages.Validation.ValidationFailed,
                        extra: $"Unsupported locales: {string.Join(",", invalid)}",
                        parameters: new { spaceId, discountId });
            }

            // Optional: validate discount type id if provided
            if (dto.DiscountTypeId.HasValue && !await _repo.DiscountTypeExistsAsync(dto.DiscountTypeId.Value))
                throw ErrorService.Exception(
                    ErrorType.Argument, nameof(UpdateAsync),
                    ErrorMessages.Validation.InvalidFormat,
                    paramName: nameof(dto.DiscountTypeId),
                    extra: "Unknown discount_type_id.",
                    parameters: new { dto.DiscountTypeId });

            var updated = await _repo.UpdateAsync(spaceId, discountId, dto, modifiedBy);
            if (updated is null)
                throw ErrorService.Exception(
                    ErrorType.NotFound, nameof(UpdateAsync),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, discountId });

            // If LocalizedPairs was provided but dto.NameKey was null, ensure key matches the final (repo enforces);
            // (No extra action needed here.)

            return updated;
        }

        public async Task DeleteAsync(int spaceId, int discountId, bool hard, string modifiedBy)
        {
            if (spaceId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(DeleteAsync),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(spaceId),
                    parameters: new { spaceId, discountId, hard });

            if (discountId <= 0)
                throw ErrorService.Exception(
                    ErrorType.Argument,
                    nameof(DeleteAsync),
                    ErrorMessages.Validation.PositiveIntRequired,
                    paramName: nameof(discountId),
                    parameters: new { spaceId, discountId, hard });

            var exists = await _repo.ExistsInSpaceAsync(spaceId, discountId);
            if (!exists)
                throw ErrorService.Exception(
                    ErrorType.NotFound,
                    nameof(DeleteAsync),
                    ErrorMessages.Http.NotFound,
                    parameters: new { spaceId, discountId });

            try
            {
                int affected = hard
                    ? await _repo.HardDeleteAsync(spaceId, discountId)
                    : await _repo.SoftExpireNowAsync(spaceId, discountId, modifiedBy);

                if (affected == 0)
                    throw new InvalidOperationException("NoRowsAffected"); // repo-agnostic sentinel
            }
            // ── Wrapper-style catches (no provider types) ──────────────────────────────
            catch (InvalidOperationException ex) when (
                ex.Message.StartsWith("FK_CONSTRAINT", StringComparison.Ordinal) ||
                ex.Message.StartsWith("DeleteBlockedByReferences", StringComparison.Ordinal))
            {
                // Hard delete blocked by dependencies → 409
                throw ErrorService.Exception(
                    ErrorType.Conflict,
                    nameof(DeleteAsync),
                    ErrorMessages.Http.Conflict,
                    parameters: new { spaceId, discountId, hard },
                    extra: "Foreign key constraints prevent hard delete. Use soft disable (expire end_at).");
            }
            catch (InvalidOperationException ex) when (
                ex.Message.StartsWith("Already committed or rolled back", StringComparison.Ordinal) ||
                ex.Message.StartsWith("TX_ALREADY_COMPLETED", StringComparison.Ordinal))
            {
                // Transaction was finalized elsewhere → 409 to signal state conflict
                throw ErrorService.Exception(
                    ErrorType.Conflict,
                    nameof(DeleteAsync),
                    ErrorMessages.Http.Conflict,
                    parameters: new { spaceId, discountId, hard },
                    extra: ex.Message);
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Equals("NoRowsAffected", StringComparison.Ordinal))
            {
                // We saw it, tried to update/delete, but nothing changed
                throw ErrorService.Exception(
                    ErrorType.InvalidOperation,
                    nameof(DeleteAsync),
                    ErrorMessages.Http.InternalServerError,
                    parameters: new { spaceId, discountId, hard },
                    extra: "No rows affected despite existence check.");
            }
        }
    }
}

