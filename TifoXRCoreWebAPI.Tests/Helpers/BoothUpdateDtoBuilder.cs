// <copyright file="BoothUpdateDtoBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>07/30/2025</date>
// <summary>Fluent builder for BoothUpdateDto to simplify and standardize test data setup.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using System.Collections.Generic;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for BoothUpdateDto. Allows chaining of overrides for MapSpot and LocalizedName.
    /// </summary>
    public class BoothUpdateDtoBuilder
    {
        private decimal _x = 0, _y = 0, _z = 0;
        private LocalizedPairs _localizedPairs = new LocalizedPairs
        {
            Key = "booth.name",
            Values = new List<LocalizedValue>
            {
                new LocalizedValue { LocaleId = "en", Value = "Booth" }
            }
        };

        /// <summary>
        /// Sets the MapSpot coordinates.
        /// </summary>
        public BoothUpdateDtoBuilder WithMapSpot(decimal x, decimal y, decimal z)
        {
            _x = x;
            _y = y;
            _z = z;
            return this;
        }

        /// <summary>
        /// Sets the LocalizedPairs with specified key and locale-value pairs.
        /// </summary>
        public BoothUpdateDtoBuilder WithLocalizedPairs(string key, Dictionary<string, string> values)
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
        public BoothUpdateDtoBuilder WithNullLocalizedPairs()
        {
            _localizedPairs = null!;
            return this;
        }

        /// <summary>
        /// Builds and returns the configured BoothUpdateDto.
        /// </summary>
        public BoothUpdateDto Build() => new BoothUpdateDto
        {
            MapSpot = new MapSpotModel { X = _x, Y = _y, Z = _z },
            LocalizedPairs = _localizedPairs
        };
    }
}
