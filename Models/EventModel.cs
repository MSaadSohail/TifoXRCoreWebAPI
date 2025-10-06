// <copyright file="EventModel.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Data for Event </summary>
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Models
{
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

        public LocalizedPairs LocalizedPairs { get; set; }
        public LocalizedPairs LocalizedDescription { get; set; }
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

        public LocalizedPairs LocalizedPairs { get; set; }
        public LocalizedPairs LocalizedDescription { get; set; }
    }

}
