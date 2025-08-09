// <copyright file="BoothCreateDtoBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/08/2025</date>
// <summary>Builder for BoothCreateDto in tests.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Builder for BoothCreateDto with sensible defaults.
    /// Defaults:
    ///   SpaceId = 1
    ///   LocalizedPairs = { Key="booth_main", Values=[("en_us","Main Booth")] }
    ///   MapSpot = (0,0,0)
    /// </summary>
    public class BoothCreateDtoBuilder
    {
        private int _spaceId = 1;
        private LocalizedPairs? _localizedPairs = new()
        {
            Key = "booth_main",
            Values = new List<LocalizedValue>
            {
                new() { LocaleId = "en_us", Value = "Main Booth" }
            }
        };
        private MapSpotModel _mapSpot = new() { X = 0, Y = 0, Z = 0 };

        public BoothCreateDtoBuilder WithSpaceId(int spaceId)
        {
            _spaceId = spaceId;
            return this;
        }

        public BoothCreateDtoBuilder WithLocalizedPairs(string key, IDictionary<string, string> valuesByLocale)
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

        public BoothCreateDtoBuilder WithNullLocalizedPairs()
        {
            _localizedPairs = null;
            return this;
        }

        public BoothCreateDtoBuilder WithMapSpot(decimal x, decimal y, decimal z)
        {
            _mapSpot = new MapSpotModel { X = x, Y = y, Z = z };
            return this;
        }

        public BoothCreateDto Build()
        {
            return new BoothCreateDto
            {
                SpaceId = _spaceId,
                LocalizedPairs = _localizedPairs!,
                MapSpot = _mapSpot
            };
        }
    }
}
