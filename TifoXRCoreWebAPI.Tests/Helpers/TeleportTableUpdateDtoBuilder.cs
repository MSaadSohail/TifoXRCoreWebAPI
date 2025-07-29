// <copyright file="TeleportTableUpdateDtoBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>07/28/2025</date>
// <summary>Fluent builder for TeleportTableUpdateDto used in tests to simplify setup of valid and edge-case DTOs.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using System.Collections.Generic;

namespace TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for TeleportTableUpdateDto to reduce repetition in tests.
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
        /// Sets LocalizedName to null to simulate missing data.
        /// </summary>
        public TeleportTableUpdateDtoBuilder WithNullLocalizedName()
        {
            _dto.LocalizedName = null;
            return this;
        }

        /// <summary>
        /// Sets Buttons to null to simulate missing button collection.
        /// </summary>
        public TeleportTableUpdateDtoBuilder WithNullButtons()
        {
            _dto.Buttons = null;
            return this;
        }

        /// <summary>
        /// Returns the configured TeleportTableUpdateDto instance.
        /// </summary>
        public TeleportTableUpdateDto Build() => _dto;
    }
}
