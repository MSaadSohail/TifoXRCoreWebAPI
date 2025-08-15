// <copyright file="EventDtoBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/11/2025</date>
// <summary>
// Fluent builder for Event request DTOs used in tests to simplify setup of valid and edge-case inputs.
// </summary>

using System;
using System.Collections.Generic;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for <see cref="Event"/> request DTOs to standardize test setup.
    /// Allows chaining of overrides for fields such as space, platform, event type,
    /// live status, and localized content.
    /// </summary>
    public class EventDtoBuilder
    {
        // Default request DTO field values
        private int _spaceId = 100;
        private int _platformTypeId = 1;
        private int _eventTypeId = 1;
        private int? _personalityId = null;
        private string _eventUrl = "https://example.com/event";
        private bool _isLive = false;
        private bool _subtitleEnabled = true;
        private int _scheduledLength = 90;
        private DateTime _scheduledStart = new DateTime(2025, 8, 20, 12, 0, 0, DateTimeKind.Utc);
        private DateTime _scheduledEnd = new DateTime(2025, 8, 20, 13, 30, 0, DateTimeKind.Utc);
        private DateTime? _actualStart = null;
        private DateTime? _actualEnd = null;
        private decimal? _rating = 4.5m;

        // Default localized name and description
        private LocalizedPairs _name = new LocalizedPairs
        {
            Key = "event_name_key",
            Values = new List<LocalizedValue>
            {
                new LocalizedValue { LocaleId = "en", Value = "Match Day" }
            }
        };

        private LocalizedPairs _desc = new LocalizedPairs
        {
            Key = "event_desc_key",
            Values = new List<LocalizedValue>
            {
                new LocalizedValue { LocaleId = "en", Value = "Kickoff event" }
            }
        };

        /// <summary>
        /// Sets the space ID for the event.
        /// </summary>
        public EventDtoBuilder WithSpaceId(int spaceId) { _spaceId = spaceId; return this; }

        /// <summary>
        /// Sets the IsLive flag for the event.
        /// </summary>
        public EventDtoBuilder WithIsLive(bool isLive) { _isLive = isLive; return this; }

        /// <summary>
        /// Builds and returns a new <see cref="Event"/> DTO instance with the current settings.
        /// </summary>
        public Event Build()
        {
            return new Event
            {
                SpaceId = _spaceId,
                PlatformTypeId = _platformTypeId,
                EventTypeId = _eventTypeId,
                PersonalityId = _personalityId,
                EventUrl = _eventUrl,
                IsLive = _isLive,
                SubtitleEnabled = _subtitleEnabled,
                ScheduledLength = _scheduledLength,
                ScheduledStart = _scheduledStart,
                ScheduledEnd = _scheduledEnd,
                ActualStart = _actualStart,
                ActualEnd = _actualEnd,
                Rating = _rating,
                LocalizedPairs = _name,
                LocalizedDescription = _desc
            };
        }
    }
}
