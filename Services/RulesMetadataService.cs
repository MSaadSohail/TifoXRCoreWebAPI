// <copyright file="RulesMetadataService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>10/01/2025</date>
// <summary>Coordinates metadata repository operations for rule authoring.</summary>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public sealed class RulesMetadataService : IRulesMetadataService
    {
        private readonly IRulesMetadataRepository _repo;

        public RulesMetadataService(IRulesMetadataRepository repo)
            => _repo = repo ?? throw new ArgumentNullException(nameof(repo));

        public Task<IReadOnlyList<EventTypeView>> GetEventTypesAsync()
            => _repo.GetEventTypesAsync();

        public Task<int> CreateEventTypeAsync(EventTypeCreateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            return _repo.InsertEventTypeAsync(dto);
        }

        public Task<bool> UpdateEventTypeAsync(EventTypeUpdateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            return _repo.UpdateEventTypeAsync(dto);
        }

        public Task<IReadOnlyList<ContextParameterView>> GetContextParametersAsync()
            => _repo.GetContextParametersAsync();

        public Task<int> CreateContextParameterAsync(ContextParameterCreateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            return _repo.InsertContextParameterAsync(dto);
        }

        public Task<bool> UpdateContextParameterAsync(ContextParameterUpdateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            return _repo.UpdateContextParameterAsync(dto);
        }

        public Task<IReadOnlyList<EventTypeParameterView>> GetEventTypeParametersAsync(int eventTypeId)
            => _repo.GetEventTypeParametersAsync(eventTypeId);

        public Task<int> CreateEventTypeParameterAsync(EventTypeParameterCreateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            return _repo.InsertEventTypeParameterAsync(dto);
        }

        public Task<bool> UpdateEventTypeParameterAsync(EventTypeParameterUpdateDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            return _repo.UpdateEventTypeParameterAsync(dto);
        }
    }
}
