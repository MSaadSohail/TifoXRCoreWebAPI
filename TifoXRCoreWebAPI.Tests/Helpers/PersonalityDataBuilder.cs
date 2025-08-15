// <copyright file="PersonalityDataBuilder.cs" company="Global Mobile Software">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/13/2025</date>
// <summary>Fluent builder for PersonalityData used in unit tests to create reusable instances.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Builds PersonalityData objects with sensible defaults and chainable overrides
    /// </summary>
    public class PersonalityDataBuilder
    {
        private int _id = 1;
        private string _name = "Test Name";
        private int? _sportId = null;
        private int? _entityId = null;

        private LocalizedPairs _country = new()
        {
            Key = "country_key",
            Values = new List<LocalizedValue>
            {
                new() { LocaleId = "en_us", Value = "United States" }
            }
        };

        private LocalizedPairs _bio = new()
        {
            Key = "bio_key",
            Values = new List<LocalizedValue>
            {
                new() { LocaleId = "en_us", Value = "Bio" }
            }
        };

        private MediaData? _media = new()
        {
            Id = "media-1",
            LinkLocalizations = new List<MediaLocalization>
            {
                new() { LocaleId = "en_us", MediaLink = "https://cdn/media.jpg" }
            }
        };

        private DateTime _creation = DateTime.UtcNow.AddDays(-1);
        private DateTime _modified = DateTime.UtcNow;
        private string _modifiedBy = "system";

        public PersonalityDataBuilder WithId(int id) { _id = id; return this; }
        public PersonalityDataBuilder WithName(string name) { _name = name; return this; }

        /// <summary>Add/replace a single (locale,value) for country.</summary>
        public PersonalityDataBuilder WithCountry(string locale, string value)
        {
            _country.Values = new List<LocalizedValue> { new() { LocaleId = locale, Value = value } };
            return this;
        }

        /// <summary>Add/replace a single (locale,value) for bio.</summary>
        public PersonalityDataBuilder WithBio(string locale, string value)
        {
            _bio.Values = new List<LocalizedValue> { new() { LocaleId = locale, Value = value } };
            return this;
        }

        /// <summary>Add/replace a single (locale, link) for media.</summary>
        public PersonalityDataBuilder WithMedia(string locale, string mediaLink)
        {
            _media ??= new MediaData { Id = "media-1", LinkLocalizations = new List<MediaLocalization>() };
            _media.LinkLocalizations = new List<MediaLocalization> { new() { LocaleId = locale, MediaLink = mediaLink } };
            return this;
        }

        /// <summary>Remove media (useful for tests that assert no media present).</summary>
        public PersonalityDataBuilder WithoutMedia()
        {
            _media = null;
            return this;
        }

        public PersonalityData Build()
        {
            return new PersonalityData
            {
                Id = _id,
                Name = _name,
                SportId = _sportId,
                EntityId = _entityId,
                LocalizedCountry = _country,
                LocalizedBio = _bio,
                Media = _media,
                CreationTime = _creation,
                ModifiedTime = _modified,
                ModifiedBy = _modifiedBy
            };
        }
    }
}
