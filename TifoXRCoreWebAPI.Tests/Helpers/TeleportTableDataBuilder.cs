// <copyright file="TeleportTableDataBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>07/29/2025</date>
// <summary>Fluent builder for TeleportTableData used in unit tests to create customizable and reusable test instances.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using System.Collections.Generic;

namespace TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for TeleportTableData to reduce repetition and improve clarity in unit tests.
    /// Allows chaining customizations for SpaceId, NameKey, localization, and button configuration.
    /// </summary>
    public class TeleportTableDataBuilder
    {
        private readonly TeleportTableData _data = new()
        {
            Id = 1,
            SpaceId = 100,
            NameKey = "table_key",
            IsActive = true,
            LocalizedName = new Dictionary<string, string> { ["en"] = "Start" },
            Buttons = new List<ButtonData>
            {
                new ButtonData
                {
                    Id = 1,
                    NameKey = "btn_key",
                    BoothToVisit = 10,
                    LocalizedName = new Dictionary<string, string> { ["en"] = "Go" }
                }
            }
        };

        /// <summary>
        /// Sets the SpaceId for the TeleportTableData. Use this to simulate different spaces in tests.
        /// </summary>
        public TeleportTableDataBuilder WithSpaceId(int spaceId)
        {
            _data.SpaceId = spaceId;
            return this;
        }

        /// <summary>
        /// Sets or clears the NameKey field to test scenarios with or without identifying keys.
        /// </summary>
        public TeleportTableDataBuilder WithNameKey(string? nameKey)
        {
            _data.NameKey = nameKey;
            return this;
        }

        /// <summary>
        /// Adds a new locale entry to the first button’s LocalizedName dictionary.
        /// Useful for testing multi-language support in button labels.
        /// </summary>
        public TeleportTableDataBuilder WithAdditionalButtonLocale(string locale, string text)
        {
            if (_data.Buttons != null && _data.Buttons.Count > 0)
            {
                _data.Buttons[0].LocalizedName[locale] = text;
            }
            return this;
        }

        /// <summary>
        /// Sets LocalizedName to null to simulate a missing or invalid localization block.
        /// </summary>
        public TeleportTableDataBuilder WithNullLocalizedName()
        {
            _data.LocalizedName = null;
            return this;
        }

        /// <summary>
        /// Sets Buttons to null to simulate a teleport table with no buttons defined.
        /// Useful for testing controller validation logic.
        /// </summary>
        public TeleportTableDataBuilder WithNullButtons()
        {
            _data.Buttons = null;
            return this;
        }

        /// <summary>
        /// Finalizes and returns the constructed TeleportTableData object with all configured overrides.
        /// </summary>
        public TeleportTableData Build() => _data;
    }
}
