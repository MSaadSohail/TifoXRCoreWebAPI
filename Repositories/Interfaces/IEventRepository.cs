// <copyright file="IEventRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Interface layer for event repository</summary>
using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface IEventRepository
    {
        Task<EventData> GetEventByIdAsync(int eventId);
        Task<EventData> CreateEventAsync(Event eventDto);
        Task<EventData> UpdateEventAsync(int eventId, Event dto);
    }

}
