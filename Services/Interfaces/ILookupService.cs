// <copyright file="ILookupService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/30/2025</date>
// <summary>Service abstraction for lookup retrieval with caching.</summary>

using System.Collections.Generic;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public interface ILookupService
    {
        Task<IReadOnlyList<IDictionary<string, object?>>> GetRewardCompositionTypesAsync();
        Task<IReadOnlyList<IDictionary<string, object?>>> GetRewardStatusesAsync();
        Task<IReadOnlyList<IDictionary<string, object?>>> GetStateTypesAsync();
        Task<IReadOnlyList<IDictionary<string, object?>>> GetRuleActionTypesAsync();
        Task<IReadOnlyList<IDictionary<string, object?>>> GetEventTypesAsync();
        Task<IReadOnlyList<IDictionary<string, object?>>> GetComparatorsAsync();
        Task<IReadOnlyList<IDictionary<string, object?>>> GetLogicalOperatorsAsync();
        Task<IReadOnlyList<IDictionary<string, object?>>> GetPathTypesAsync();
        Task<IReadOnlyList<IDictionary<string, object?>>> GetProviderTypesAsync();
        Task<IReadOnlyList<IDictionary<string, object?>>> GetContextParametersAsync();
        Task<IReadOnlyList<IDictionary<string, object?>>> GetEventTypeParametersAsync(int eventTypeId);
    }
}
