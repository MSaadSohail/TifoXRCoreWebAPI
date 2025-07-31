// <copyright file="BoothModelBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>07/30/2025</date>
// <summary>Fluent builder for BoothModel to simplify and standardize setup in unit tests.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using System.Collections.Generic;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for BoothModel. Supports custom setup of Id, SpaceId, MapSpot, and LocalizedName.
    /// </summary>
    public class BoothModelBuilder
    {
        private int _id = 1;
        private int _spaceId = 100;
        private int _mapSpotId = 0;
        private MapSpotModel _mapSpot = new MapSpotModel { X = 0, Y = 0, Z = 0 };
        private LocalizedPairs _localizedPairs = new LocalizedPairs
        {
            Key = "booth.name",
            Values = new List<LocalizedValue>
            {
                new LocalizedValue { LocaleId = "en", Value = "Booth Name" }
            }
        };

        /// <summary>
        /// Sets the Id of the booth.
        /// </summary>
        public BoothModelBuilder WithId(int id)
        {
            _id = id;
            return this;
        }

        /// <summary>
        /// Sets the SpaceId of the booth.
        /// </summary>
        public BoothModelBuilder WithSpaceId(int spaceId)
        {
            _spaceId = spaceId;
            return this;
        }

        /// <summary>
        /// Sets the MapSpotId and coordinates of the booth.
        /// </summary>
        public BoothModelBuilder WithMapSpot(int mapSpotId, decimal x, decimal y, decimal z)
        {
            _mapSpotId = mapSpotId;
            _mapSpot = new MapSpotModel { X = x, Y = y, Z = z };
            return this;
        }

        /// <summary>
        /// Sets the LocalizedPairs with specified key and locale–value pairs.
        /// </summary>
        public BoothModelBuilder WithLocalizedPairs(string key, Dictionary<string, string> values)
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
        public BoothModelBuilder WithNullLocalizedPairs()
        {
            _localizedPairs = null!;
            return this;
        }

        /// <summary>
        /// Builds and returns the configured BoothModel.
        /// </summary>
        public BoothModel Build()
        {
            return new BoothModel
            {
                Id = _id,
                SpaceId = _spaceId,
                MapSpotId = _mapSpotId,
                MapSpot = _mapSpot,
                LocalizedPairs = _localizedPairs
            };
        }
    }
}
