// <copyright file="LookupService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved</copyright>
// <author>Saad Sohail</author>
// <date>09/30/2025</date>
// <summary>Lookup service with in-memory caching.</summary>

using Microsoft.Extensions.Caching.Memory;
//
using GMS.TifoXRCoreWebAPI.Repositories;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public sealed class LookupService : ILookupService
    {
        private readonly ILookupRepository _repo;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public LookupService(ILookupRepository repo, IMemoryCache cache)
        {
            _repo = repo;
            _cache = cache;
        }

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetRewardCompositionTypesAsync()
            => GetCachedAsync("lookups:reward-composition-types", _repo.GetRewardCompositionTypesAsync);

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetRewardStatusesAsync()
            => GetCachedAsync("lookups:reward-status", _repo.GetRewardStatusesAsync);

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetStateTypesAsync()
            => GetCachedAsync("lookups:state-types", _repo.GetStateTypesAsync);

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetRuleActionTypesAsync()
            => GetCachedAsync("lookups:rule-action-types", _repo.GetRuleActionTypesAsync);

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetEventTypesAsync()
            => GetCachedAsync("lookups:event-types", _repo.GetEventTypesAsync);

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetComparatorsAsync()
            => GetCachedAsync("lookups:comparators", _repo.GetComparatorsAsync);

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetLogicalOperatorsAsync()
            => GetCachedAsync("lookups:logical-operators", _repo.GetLogicalOperatorsAsync);

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetPathTypesAsync()
            => GetCachedAsync("lookups:path-types", _repo.GetPathTypesAsync);

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetProviderTypesAsync()
            => GetCachedAsync("lookups:provider-types", _repo.GetProviderTypesAsync);

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetContextParametersAsync()
            => GetCachedAsync("lookups:context-parameters", _repo.GetContextParametersAsync);

        public Task<IReadOnlyList<IDictionary<string, object?>>> GetEventTypeParametersAsync(int eventTypeId)
            => GetCachedAsync($"lookups:event-type-parameters:{eventTypeId}", () => _repo.GetEventTypeParametersAsync(eventTypeId));

        private Task<IReadOnlyList<IDictionary<string, object?>>> GetCachedAsync(string key, Func<Task<IReadOnlyList<IDictionary<string, object?>>>> factory)
        {
            if (_cache.TryGetValue(key, out IReadOnlyList<IDictionary<string, object?>>? cached))
                return Task.FromResult(cached);

            return PopulateAsync(key, factory);
        }

        private async Task<IReadOnlyList<IDictionary<string, object?>>> PopulateAsync(string key, Func<Task<IReadOnlyList<IDictionary<string, object?>>>> factory)
        {
            var value = await factory();
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration
            };

            _cache.Set(key, value, options);
            return value;
        }
    }
}
