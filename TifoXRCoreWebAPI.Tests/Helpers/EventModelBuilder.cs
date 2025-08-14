// <copyright file="EventModelBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/11/2025</date>
// <summary>Fluent builder for EventData used in unit tests to create customizable and reusable test instances.</summary>

using System;
using System.Collections.Generic;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for <see cref="EventData"/> to reduce repetition and improve clarity in unit tests.
    /// of key EventData fields such as localization, schedule, and metadata.
    /// </summary>
    public class EventModelBuilder
    {
        // Default field values for a new EventData instance.
        private int _id = 1;
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
        /// Sets the Event ID.
        /// </summary>
        public EventModelBuilder WithId(int id) { _id = id; return this; }

        /// <summary>
        /// Adds an additional localized name entry.
        /// </summary>
        public EventModelBuilder WithAdditionalNameLocale(string locale, string text)
        {
            _name.Values.Add(new LocalizedValue { LocaleId = locale, Value = text });
            return this;
        }

        /// <summary>
        /// Adds an additional localized description entry.
        /// </summary>
        public EventModelBuilder WithAdditionalDescLocale(string locale, string text)
        {
            _desc.Values.Add(new LocalizedValue { LocaleId = locale, Value = text });
            return this;
        }

        /// <summary>
        /// Copies common fields from an Event request DTO into the builder’s state.
        /// Useful for asserting repository return values match what was sent.
        /// </summary>
        public EventModelBuilder FromDto(Event dto)
        {
            _platformTypeId = dto.PlatformTypeId;
            _eventTypeId = dto.EventTypeId;
            _personalityId = dto.PersonalityId;
            _eventUrl = dto.EventUrl;
            _isLive = dto.IsLive;
            _subtitleEnabled = dto.SubtitleEnabled;
            _scheduledLength = dto.ScheduledLength;
            _scheduledStart = dto.ScheduledStart;
            _scheduledEnd = dto.ScheduledEnd;
            _actualStart = dto.ActualStart;
            _actualEnd = dto.ActualEnd;
            _rating = dto.Rating;
            _name = dto.LocalizedPairs;
            _desc = dto.LocalizedDescription;
            return this;
        }

        /// <summary>
        /// Constructs and returns an <see cref="EventData"/> instance with the configured values.
        /// </summary>
        public EventData Build()
        {
            return new EventData
            {
                Id = _id,
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