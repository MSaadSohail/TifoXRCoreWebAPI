// <copyright file="BoothCreateDtoBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/11/2025</date>
// <summary>Fluent builder for BoothCreateDto used in tests for clean and reusable setup.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for BoothCreateDto.
    /// </summary>
    public class BoothCreateDtoBuilder
    {
        private int _spaceId = 1;
        private string _key = "booth_key";
        private readonly List<LocalizedValue> _values = new()
        {
            new LocalizedValue { LocaleId = "en_us", Value = "Booth Name" }
        };
        private MapSpotModel _mapSpot = new() { X = 1.1m, Y = 2.2m, Z = 3.3m };

        /// <summary>
        /// Sets the SpaceId field for the DTO.
        /// </summary>
        public BoothCreateDtoBuilder WithSpaceId(int spaceId)
        {
            _spaceId = spaceId;
            return this;
        }

        /// <summary>
        /// Sets the i18n key for the booth's LocalizedPairs.
        /// </summary>
        public BoothCreateDtoBuilder WithKey(string key)
        {
            _key = key;
            return this;
        }

        /// <summary>
        /// Adds a (locale, value) entry to the LocalizedPairs.Values collection.
        /// </summary>
        public BoothCreateDtoBuilder WithLocalizedPair(string locale, string value)
        {
            _values.Add(new LocalizedValue { LocaleId = locale, Value = value });
            return this;
        }

        /// <summary>
        /// Sets the MapSpot coordinates for the new booth.
        /// </summary>
        public BoothCreateDtoBuilder WithMapSpot(decimal x, decimal y, decimal z)
        {
            _mapSpot = new MapSpotModel { X = x, Y = y, Z = z };
            return this;
        }

        /// <summary>
        /// Builds a BoothCreateDto instance with the configured state.
        /// </summary>
        public BoothCreateDto Build()
        {
            return new BoothCreateDto
            {
                SpaceId = _spaceId,
                LocalizedPairs = new LocalizedPairs
                {
                    Key = _key,
                    Values = _values
                },
                MapSpot = _mapSpot
            };
        }
    }
}
