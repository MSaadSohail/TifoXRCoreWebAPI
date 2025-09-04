// <copyright file="PersonalityCreateDtoBuilder.cs" company="Global Mobile Software">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/13/2025</date>
// <summary>Fluent builder for PersonalityCreateDto to simplify controller test setup.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Builds PersonalityCreateDto objects with chainable overrides,
    /// providing helpers for localized fields and optional media.
    /// </summary>
    public class PersonalityCreateDtoBuilder
    {
        private string _name = "Test Personality";
        private int? _sportId = null;
        private int? _entityId = null;
        private int _spaceId = 1;
        private string? _modifiedBy = "system";

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

        private MediaUpdateDto? _media = new()
        {
            MediaTypeId = 1,
            TextKey = null,
            DescriptionKey = null,
            LinkLocalizations = new List<MediaLocalization>
            {
                new() { LocaleId = "en_us", MediaLink = "https://cdn/media.jpg" }
            }
        };

        public PersonalityCreateDtoBuilder WithName(string name) { _name = name; return this; }

        public PersonalityCreateDtoBuilder WithSpaceId(int spaceId) { _spaceId = spaceId; return this; }

        /// <summary>Replace country localization with a single (locale,value).</summary>
        public PersonalityCreateDtoBuilder WithCountry(string locale, string value)
        {
            _country.Values = new List<LocalizedValue> { new() { LocaleId = locale, Value = value } };
            return this;
        }

        /// <summary>Replace bio localization with a single (locale,value).</summary>
        public PersonalityCreateDtoBuilder WithBio(string locale, string value)
        {
            _bio.Values = new List<LocalizedValue> { new() { LocaleId = locale, Value = value } };
            return this;
        }

        /// <summary>Remove media to test "no media" paths.</summary>
        public PersonalityCreateDtoBuilder WithoutMedia()
        {
            _media = null;
            return this;
        }

        public PersonalityCreateDto Build()
        {
            return new PersonalityCreateDto
            {
                Name = _name,
                SportId = _sportId,
                EntityId = _entityId,
                SpaceId = _spaceId,
                ModifiedBy = _modifiedBy,
                LocalizedCountry = _country,
                LocalizedBio = _bio,
                Media = _media
            };
        }
    }
}
