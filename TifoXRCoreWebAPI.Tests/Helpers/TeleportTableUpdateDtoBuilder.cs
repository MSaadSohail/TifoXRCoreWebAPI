// <copyright file="TeleportTableUpdateDtoBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>07/29/2025</date>
// <summary>Fluent builder for TeleportTableUpdateDto used in tests to simplify setup of valid and edge-case DTOs.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using System.Collections.Generic;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for TeleportTableUpdateDto to simplify and standardize DTO creation in unit tests.
    /// Allows chaining of overrides for localization and button configuration.
    /// </summary>
    public class TeleportTableUpdateDtoBuilder
    {
        private bool _isActive = false;
        private string _nameKey = "table_key";
        private LocalizedPairs _localizedPairs = new LocalizedPairs
        {
            Key = "table_key",
            Values = new List<LocalizedValue>
            {
                new LocalizedValue { LocaleId = "en", Value = "Updated" }
            }
        };
        private List<ButtonUpdateDto> _buttons = new List<ButtonUpdateDto>
        {
            new ButtonUpdateDto
            {
                Id = 1,
                NameKey = "btn_key",
                IsActive = true,
                MapSpot = new MapSpotData { Id = 99, X = 0, Y = 0, Z = 0 },
                LocalizedPairs = new LocalizedPairs
                {
                    Key = "btn_key",
                    Values = new List<LocalizedValue>
                    {
                        new LocalizedValue { LocaleId = "en", Value = "Teleport" }
                    }
                }
            }
        };

        /// <summary>
        /// Sets LocalizedPairs to null to simulate a DTO missing localization.
        /// Useful for testing validation behavior on missing required fields.
        /// </summary>
        public TeleportTableUpdateDtoBuilder WithNullLocalizedPairs()
        {
            _localizedPairs = null!;
            return this;
        }

        /// <summary>
        /// Sets Buttons to null to simulate a DTO missing its button definitions.
        /// Useful for testing how the controller handles missing collections.
        /// </summary>
        public TeleportTableUpdateDtoBuilder WithNullButtons()
        {
            _buttons = null!;
            return this;
        }

        /// <summary>
        /// Finalizes and returns a new TeleportTableUpdateDto instance
        /// with any overridden values applied.
        /// </summary>
        public TeleportTableUpdateDto Build()
        {
            return new TeleportTableUpdateDto
            {
                IsActive = _isActive,
                NameKey = _nameKey,
                LocalizedPairs = _localizedPairs,
                Buttons = _buttons
            };
        }
    }
}
