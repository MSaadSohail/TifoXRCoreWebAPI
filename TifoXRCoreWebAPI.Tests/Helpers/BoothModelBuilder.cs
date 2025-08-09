// <copyright file="BoothModelBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/08/2025</date>
// <summary>Builder for BoothModel in tests.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Builder for BoothModel with sensible defaults.
    /// Defaults:
    ///   Id = 1, SpaceId = 1, MapSpotId = 10
    ///   MapSpot = (0,0,0)
    ///   LocalizedPairs = { Key="booth_main", Values=[("en_us","Main Booth")] }
    /// </summary>
    public class BoothModelBuilder
    {
        private int _id = 1;
        private int _spaceId = 1;
        private int _mapSpotId = 10;
        private MapSpotModel? _mapSpot = new() { X = 0, Y = 0, Z = 0 };
        private LocalizedPairs _localizedPairs = new()
        {
            Key = "booth_main",
            Values = new List<LocalizedValue>
            {
                new() { LocaleId = "en_us", Value = "Main Booth" }
            }
        };

        public BoothModelBuilder WithId(int id)
        {
            _id = id;
            return this;
        }

        public BoothModelBuilder WithSpaceId(int spaceId)
        {
            _spaceId = spaceId;
            return this;
        }

        public BoothModelBuilder WithMapSpotId(int mapSpotId)
        {
            _mapSpotId = mapSpotId;
            return this;
        }

        public BoothModelBuilder WithMapSpot(decimal x, decimal y, decimal z)
        {
            _mapSpot = new MapSpotModel { X = x, Y = y, Z = z };
            return this;
        }

        public BoothModelBuilder WithoutMapSpot()
        {
            _mapSpot = null;
            return this;
        }

        public BoothModelBuilder WithLocalizedPairs(string key, IDictionary<string, string> valuesByLocale)
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

        public BoothModelBuilder WithLocalizedPairsKey(string key)
        {
            _localizedPairs.Key = key;
            return this;
        }

        public BoothModelBuilder AddLocale(string localeId, string value)
        {
            _localizedPairs.Values.Add(new LocalizedValue { LocaleId = localeId, Value = value });
            return this;
        }

        public BoothModelBuilder ClearLocales()
        {
            _localizedPairs.Values.Clear();
            return this;
        }

        public BoothModel Build()
        {
            return new BoothModel
            {
                Id = _id,
                SpaceId = _spaceId,
                MapSpotId = _mapSpotId,
                MapSpot = _mapSpot,
                LocalizedPairs = new LocalizedPairs
                {
                    Key = _localizedPairs.Key,
                    Values = _localizedPairs.Values.ToList()
                }
            };
        }
    }
}
