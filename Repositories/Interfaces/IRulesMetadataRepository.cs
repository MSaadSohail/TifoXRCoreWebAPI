// <copyright file="IRulesMetadataRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>10/01/2025</date>
// <summary>Data access for rules metadata tables (re_* lookups).</summary>

using System.Collections.Generic;
using System.Threading.Tasks;
using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public interface IRulesMetadataRepository
    {
        Task<IReadOnlyList<EventTypeView>> GetEventTypesAsync();
        Task<int> InsertEventTypeAsync(EventTypeCreateDto dto);
        Task<bool> UpdateEventTypeAsync(EventTypeUpdateDto dto);

        Task<IReadOnlyList<ContextParameterView>> GetContextParametersAsync();
        Task<int> InsertContextParameterAsync(ContextParameterCreateDto dto);
        Task<bool> UpdateContextParameterAsync(ContextParameterUpdateDto dto);

        Task<IReadOnlyList<EventTypeParameterView>> GetEventTypeParametersAsync(int eventTypeId);
        Task<int> InsertEventTypeParameterAsync(EventTypeParameterCreateDto dto);
        Task<bool> UpdateEventTypeParameterAsync(EventTypeParameterUpdateDto dto);
    }
}
