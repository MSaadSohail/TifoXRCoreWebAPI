// <copyright file="BoothCreateDtoBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>07/30/2025</date>
// <summary>Fluent builder for BoothCreateDto to simplify and standardize test data setup.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using System.Collections.Generic;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for BoothCreateDto. Allows chaining of overrides for SpaceId, MapSpot, and LocalizedName.
    /// </summary>
    public class BoothCreateDtoBuilder
    {
        private int _spaceId = 100;
        private MapSpotModel _mapSpot = new MapSpotModel { X = 0, Y = 0, Z = 0 };
        private LocalizedPairs _localizedPairs = new LocalizedPairs
        {
            Key = "booth.name",
            Values = new List<LocalizedValue>
            {
                new LocalizedValue { LocaleId = "en", Value = "Booth" }
            }
        };

        /// <summary>
        /// Sets the SpaceId.
        /// </summary>
        public BoothCreateDtoBuilder WithSpaceId(int spaceId)
        {
            _spaceId = spaceId;
            return this;
        }

        /// <summary>
        /// Sets the MapSpot coordinates.
        /// </summary>
        public BoothCreateDtoBuilder WithMapSpot(decimal x, decimal y, decimal z)
        {
            _mapSpot = new MapSpotModel { X = x, Y = y, Z = z };
            return this;
        }

        /// <summary>
        /// Sets the LocalizedPairs with specified key and locale-value pairs.
        /// </summary>
        public BoothCreateDtoBuilder WithLocalizedPairs(string key, Dictionary<string, string> values)
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
        /// Sets the LocalizedPairs to null.
        /// </summary>
        public BoothCreateDtoBuilder WithNullLocalizedPairs()
        {
            _localizedPairs = null!;
            return this;
        }

        /// <summary>
        /// Builds and returns the configured BoothCreateDto.
        /// </summary>
        public BoothCreateDto Build() => new BoothCreateDto
        {
            SpaceId = _spaceId,
            MapSpot = _mapSpot,
            LocalizedPairs = _localizedPairs
        };
    }
}