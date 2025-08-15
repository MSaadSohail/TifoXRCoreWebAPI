// <copyright file="BoothControllerTests.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/11/2025</date>
// <summary>Fluent builder for BoothModel used in tests to create reusable and expressive fixtures.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for BoothModel instances to keep tests concise and consistent.
    /// Matches the builder pattern used for Teleport entities.
    /// </summary>
    public class BoothModelBuilder
    {
        private int _id = 1;
        private int _spaceId = 100;
        private int _mapSpotId = 10;
        private string _key = "booth_key";
        private MapSpotModel? _mapSpot = new() { X = 0, Y = 0, Z = 0 };

        /// <summary>
        /// Backing list for LocalizedPairs.Values; includes a default value in English (US).
        /// </summary>
        private readonly List<LocalizedValue> _values = new()
        {
            new LocalizedValue { LocaleId = "en_us", Value = "Booth EN" }
        };

        /// <summary>
        /// Sets the BoothModel.Id field.
        /// </summary>
        public BoothModelBuilder WithId(int id)
        {
            _id = id;
            return this;
        }

        /// <summary>
        /// Sets the BoothModel.SpaceId field.
        /// </summary>
        public BoothModelBuilder WithSpaceId(int spaceId)
        {
            _spaceId = spaceId;
            return this;
        }

        /// <summary>
        /// Sets the LocalizedPairs.Key field.
        /// </summary>
        public BoothModelBuilder WithKey(string key)
        {
            _key = key;
            return this;
        }

        /// <summary>
        /// Sets the MapSpotModel (x, y, z) included in the BoothModel.
        /// </summary>
        public BoothModelBuilder WithMapSpot(decimal x, decimal y, decimal z)
        {
            _mapSpot = new MapSpotModel { X = x, Y = y, Z = z };
            return this;
        }

        /// <summary>
        /// Adds an additional (locale, value) pair to the booth's LocalizedPairs.Values.
        /// </summary>
        public BoothModelBuilder WithLocalizedPair(string locale, string value)
        {
            _values.Add(new LocalizedValue { LocaleId = locale, Value = value });
            return this;
        }

        /// <summary>
        /// Builds a fresh BoothModel instance with the configured properties and localization values.
        /// </summary>
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
                    Key = _key,
                    Values = _values
                }
            };
        }
    }
}
