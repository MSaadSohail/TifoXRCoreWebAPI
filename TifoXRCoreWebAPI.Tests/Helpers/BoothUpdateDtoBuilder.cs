// <copyright file="BoothUpdateDtoBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/08/2025</date>
// <summary>Builder for BoothUpdateDto in tests.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Builder for BoothUpdateDto with sensible defaults.
    /// Defaults:
    ///   LocalizedPairs = { Key="booth_updated", Values=[("en_us","Updated Booth")] }
    ///   MapSpot = (1,2,3)
    /// </summary>
    public class BoothUpdateDtoBuilder
    {
        private LocalizedPairs? _localizedPairs = new()
        {
            Key = "booth_updated",
            Values = new List<LocalizedValue>
            {
                new() { LocaleId = "en_us", Value = "Updated Booth" }
            }
        };
        private MapSpotModel _mapSpot = new() { X = 1, Y = 2, Z = 3 };

        public BoothUpdateDtoBuilder WithLocalizedPairs(string key, IDictionary<string, string> valuesByLocale)
        {
            _localizedPairs = new LocalizedPairs
            {
                Key = key,
                Values = valuesByLocale.Select(kv => new LocalizedValue
                {
                    LocaleId = kv.Key,
                    Value = kv.Value
                }).ToList()
            };
            return this;
        }

        public BoothUpdateDtoBuilder WithNullLocalizedPairs()
        {
            _localizedPairs = null;
            return this;
        }

        public BoothUpdateDtoBuilder WithMapSpot(decimal x, decimal y, decimal z)
        {
            _mapSpot = new MapSpotModel { X = x, Y = y, Z = z };
            return this;
        }

        public BoothUpdateDto Build()
        {
            return new BoothUpdateDto
            {
                LocalizedPairs = _localizedPairs!,
                MapSpot = _mapSpot
            };
        }
    }
}
