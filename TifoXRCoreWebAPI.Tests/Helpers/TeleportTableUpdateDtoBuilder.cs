// <copyright file="TeleportTableUpdateDtoBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>07/29/2025</date>
// <summary>Fluent builder for TeleportTableUpdateDto used in tests to simplify setup of valid and edge-case DTOs.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using System.Collections.Generic;

namespace TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for TeleportTableUpdateDto to simplify and standardize DTO creation in unit tests.
    /// Allows chaining of overrides for localization and button configuration.
    /// </summary>
    public class TeleportTableUpdateDtoBuilder
    {
        private readonly TeleportTableUpdateDto _dto = new()
        {
            IsActive = false,
            NameKey = "table_key",
            LocalizedName = new Dictionary<string, string>
            {
                ["en"] = "Updated"
            },
            Buttons = new List<ButtonUpdateDto>
            {
                new ButtonUpdateDto
                {
                    Id = 1,
                    NameKey = "btn_key",
                    BoothToVisit = 99,
                    LocalizedName = new Dictionary<string, string>
                    {
                        ["en"] = "Teleport"
                    }
                }
            }
        };

        /// <summary>
        /// Sets LocalizedName to null to simulate a DTO missing localization.
        /// Useful for testing validation behavior on missing required fields.
        /// </summary>
        public TeleportTableUpdateDtoBuilder WithNullLocalizedName()
        {
            _dto.LocalizedName = null;
            return this;
        }

        /// <summary>
        /// Sets Buttons to null to simulate a DTO missing its button definitions.
        /// Useful for testing how the controller handles missing collections.
        /// </summary>
        public TeleportTableUpdateDtoBuilder WithNullButtons()
        {
            _dto.Buttons = null;
            return this;
        }

        /// <summary>
        /// Finalizes and returns the configured TeleportTableUpdateDto instance
        /// with any overridden values applied.
        /// </summary>
        public TeleportTableUpdateDto Build() => _dto;
    }
}
