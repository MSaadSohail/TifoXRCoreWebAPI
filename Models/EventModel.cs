
using TifoXRWebApi.Models.Common;

namespace TifoXRWebApi.Models
{//Event
    public class Event
    {
        public int SpaceId { get; set; }
        public int PlatformTypeId { get; set; }
        public int EventTypeId { get; set; }
        public int? PersonalityId { get; set; }
        public string EventUrl { get; set; }
        public bool IsLive { get; set; }
        public bool SubtitleEnabled { get; set; }
        public int ScheduledLength { get; set; }
        public DateTime ScheduledStart { get; set; }
        public DateTime ScheduledEnd { get; set; }
        public DateTime? ActualStart { get; set; }
        public DateTime? ActualEnd { get; set; }
        public decimal? Rating { get; set; }

        public LocalizedName LocalizedName { get; set; }
        public LocalizedName LocalizedDescription { get; set; }
    }


    public class EventData
    {
        public int Id { get; set; }
        public int PlatformTypeId { get; set; }
        public int EventTypeId { get; set; }
        public int? PersonalityId { get; set; }
        public string EventUrl { get; set; }
        public bool IsLive { get; set; }
        public bool SubtitleEnabled { get; set; }
        public int ScheduledLength { get; set; }
        public DateTime ScheduledStart { get; set; }
        public DateTime ScheduledEnd { get; set; }
        public DateTime? ActualStart { get; set; }
        public DateTime? ActualEnd { get; set; }
        public decimal? Rating { get; set; }

        public LocalizedName LocalizedName { get; set; }
        public LocalizedName LocalizedDescription { get; set; }
    }

}
