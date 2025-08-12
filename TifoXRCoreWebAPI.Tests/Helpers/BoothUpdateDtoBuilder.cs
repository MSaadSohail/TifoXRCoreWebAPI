// <copyright file="BoothUpdateDtoBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/11/2025</date>
// <summary>Fluent builder for BoothUpdateDto used in tests to simplify setup of valid and edge-case DTOs.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for BoothUpdateDto to reduce repetition in test setup and to model invalid DTO states cleanly.
    /// Mirrors the approach used by TeleportTableUpdateDtoBuilder.
    /// </summary>
    public class BoothUpdateDtoBuilder
    {
        private string _key = "booth_key";
        private List<LocalizedValue>? _values = new()
        {
            new LocalizedValue { LocaleId = "en_us", Value = "Booth Name" }
        };
        private MapSpotModel? _mapSpot = new() { X = 1.0m, Y = 2.0m, Z = 3.0m };

        private bool _nullLocalizedPairs = false;
        private bool _nullMapSpot = false;

        /// <summary>
        /// Explicitly nulls the LocalizedPairs object to model invalid input scenarios.
        /// </summary>
        public BoothUpdateDtoBuilder WithNullLocalizedPairs()
        {
            _nullLocalizedPairs = true;
            return this;
        }

        /// <summary>
        /// Explicitly nulls the MapSpot object to model invalid input scenarios.
        /// </summary>
        public BoothUpdateDtoBuilder WithNullMapSpot()
        {
            _nullMapSpot = true;
            return this;
        }

        /// <summary>
        /// Builds a BoothUpdateDto instance with the configured values and null toggles applied.
        /// </summary>
        public BoothUpdateDto Build()
        {
            return new BoothUpdateDto
            {
                LocalizedPairs = _nullLocalizedPairs
                    ? null!
                    : new LocalizedPairs
                    {
                        Key = _key,
                        Values = _values ?? new List<LocalizedValue>()
                    },
                MapSpot = _nullMapSpot ? null! : _mapSpot!
            };
        }
    }
}
