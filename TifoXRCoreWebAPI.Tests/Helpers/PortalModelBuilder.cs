// <copyright file="PortalModelBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>07/29/2025</date>
// <summary>Fluent builder for PortalModel used in tests to create customizable and reusable test instances.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for PortalModel to reduce repetition and improve clarity in unit tests.
    /// Allows chaining of field customizations for PortalId, SpaceId, localization, media, etc.
    /// </summary>
    public class PortalModelBuilder
    {
        private int _portalId = 1;
        private int _spaceId = 100;
        private int? _boothId = 200;
        private int? _portalTypeId = 1;
        private int? _eventId = 10;
        private string _externalLink = "https://example.com";

        private LocalizedPairs _localizedPairs = new LocalizedPairs
        {
            Key = "portal.name",
            Values = new List<LocalizedValue>
            {
                new LocalizedValue { LocaleId = "en_us", Value = "Test Portal" }
            }
        };

        private string _corrMediaId = "m1";
        private int _corrMediaTypeId = 1;
        private List<MediaLocalization> _corrMediaLinks = new List<MediaLocalization>();

        private string _thumbMediaId = "m2";
        private int _thumbMediaTypeId = 2;
        private List<MediaLocalization> _thumbMediaLinks = new List<MediaLocalization>();

        /// <summary>
        /// Sets the PortalId field.
        /// </summary>
        public PortalModelBuilder WithId(int id) 
        {
            _portalId = id;
            return this;
        }

        /// <summary>
        /// Sets the SpaceId field.
        /// </summary>
        public PortalModelBuilder WithSpaceId(int spaceId) 
        {
            _spaceId = spaceId;
            return this;
        }

        /// <summary>
        /// Sets the BoothId field.
        /// </summary>
        public PortalModelBuilder WithBoothId(int boothId) 
        {
            _boothId = boothId;
            return this;
        }

        /// <summary>
        /// Sets the EventId field.
        /// </summary>
        public PortalModelBuilder WithEventId(int eventId) 
        {
            _eventId = eventId;
            return this;
        }

        /// <summary>
        /// Sets the PortalTypeId field.
        /// </summary>
        public PortalModelBuilder WithPortalTypeId(int portalTypeId) 
        {
            _portalTypeId = portalTypeId;
            return this;
        }

        /// <summary>
        /// Sets the ExternalLink field.
        /// </summary>
        public PortalModelBuilder WithExternalLink(string link) 
        {
            _externalLink = link;
            return this;
        }

        /// <summary>
        /// Sets the LocalizedName with a key and localized value.
        /// </summary>
        public PortalModelBuilder WithLocalizedPairs(string key, Dictionary<string, string> values)
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
        /// Finalizes and returns the constructed PortalModel.
        /// </summary>
        public PortalModel Build()
        {
            return new PortalModel
            {
                Id = _portalId,
                SpaceId = _spaceId,
                BoothId = _boothId,
                PortalTypeId = _portalTypeId,
                EventId = _eventId,
                LocalizedPairs = _localizedPairs,
                ExternalLink = _externalLink,
                CorrespondingMedia = new MediaData
                {
                    Id = _corrMediaId,
                    MediaTypeId = _corrMediaTypeId,
                    LinkLocalizations = new List<MediaLocalization>(_corrMediaLinks)
                },
                ThumbnailMedia = new MediaData
                {
                    Id = _thumbMediaId,
                    MediaTypeId = _thumbMediaTypeId,
                    LinkLocalizations = new List<MediaLocalization>(_thumbMediaLinks)
                }
            };
        }
    }
}
