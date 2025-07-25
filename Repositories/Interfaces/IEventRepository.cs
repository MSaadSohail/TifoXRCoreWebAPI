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
