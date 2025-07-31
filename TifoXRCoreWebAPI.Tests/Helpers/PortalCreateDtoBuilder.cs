// <copyright file="PortalCreateDtoBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>07/29/2025</date>
// <summary>Fluent builder for PortalCreateDto used in unit tests to simplify setup of valid and edge-case DTOs.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for PortalCreateDto to simplify and standardize DTO creation in unit tests.
    /// Allows chaining of overrides for PortalTypeId, link, and localized name.
    /// </summary>
    public class PortalCreateDtoBuilder
    {
        private int? _boothId;
        private int? _portalTypeId = 1;
        private int? _eventId;
        private string _externalLink = "https://example.com";
        private LocalizedPairs _localizedPairs = new LocalizedPairs
        {
            Key = "portal.name",
            Values = new List<LocalizedValue>
            {
                new LocalizedValue { LocaleId = "en_us", Value = "Test Portal" }
            }
        };
        private MediaCreateDto? _corrMedia;
        private MediaCreateDto? _thumbMedia;

        /// <summary>
        /// Sets the BoothId for the DTO.
        /// </summary>
        public PortalCreateDtoBuilder WithBoothId(int boothId)
        {
            _boothId = boothId;
            return this;
        }

        /// <summary>
        /// Sets the PortalTypeId for the DTO.
        /// </summary>
        public PortalCreateDtoBuilder WithPortalTypeId(int id)
        {
            _portalTypeId = id;
            return this;
        }

        /// <summary>
        /// Sets the EventId for the DTO.
        /// </summary>
        public PortalCreateDtoBuilder WithEventId(int eventId)
        {
            _eventId = eventId;
            return this;
        }

        /// <summary>
        /// Sets the ExternalLink for the DTO.
        /// </summary>
        public PortalCreateDtoBuilder WithLink(string link)
        {
            _externalLink = link;
            return this;
        }

        /// <summary>
        /// Sets the LocalizedPairs (i18n) for the DTO.
        /// </summary>
        public PortalCreateDtoBuilder WithLocalizedPairs(string key, Dictionary<string, string> values)
        {
            _localizedPairs = new LocalizedPairs
            {
                Key = key,
                Values = values
                    .Select(kv => new LocalizedValue { LocaleId = kv.Key, Value = kv.Value })
                    .ToList()
            };
            return this;
        }

        /// <summary>
        /// Builds and returns the configured PortalCreateDto.
        /// </summary>
        public PortalCreateDto Build() =>
            new PortalCreateDto
            {
                BoothId = _boothId,
                PortalTypeId = _portalTypeId,
                EventId = _eventId,
                LocalizedPairs = _localizedPairs,
                ExternalLink = _externalLink,
                CorrespondingMedia = _corrMedia,
                ThumbnailMedia = _thumbMedia
            };
    }
}
