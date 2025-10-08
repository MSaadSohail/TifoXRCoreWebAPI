// <copyright file="IRulesMetadataService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>10/01/2025</date>
// <summary>Application service for rules metadata management.</summary>

using System.Collections.Generic;
using System.Threading.Tasks;
using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public interface IRulesMetadataService
    {
        Task<IReadOnlyList<EventTypeView>> GetEventTypesAsync();
        Task<int> CreateEventTypeAsync(EventTypeCreateDto dto);
        Task<bool> UpdateEventTypeAsync(EventTypeUpdateDto dto);

        Task<IReadOnlyList<ContextParameterView>> GetContextParametersAsync();
        Task<int> CreateContextParameterAsync(ContextParameterCreateDto dto);
        Task<bool> UpdateContextParameterAsync(ContextParameterUpdateDto dto);

        Task<IReadOnlyList<EventTypeParameterView>> GetEventTypeParametersAsync(int eventTypeId);
        Task<int> CreateEventTypeParameterAsync(EventTypeParameterCreateDto dto);
        Task<bool> UpdateEventTypeParameterAsync(EventTypeParameterUpdateDto dto);
    }
}
