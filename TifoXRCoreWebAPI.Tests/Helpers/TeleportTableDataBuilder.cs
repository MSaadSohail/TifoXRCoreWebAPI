// <copyright file="TeleportTableDataBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>07/28/2025</date>
// <summary>Fluent builder for TeleportTableData used in unit tests to create customizable and reusable test instances.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using System.Collections.Generic;

namespace TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for TeleportTableData to reduce repetition in tests.
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
        /// Override the SpaceId property.
        /// </summary>
        public TeleportTableDataBuilder WithSpaceId(int spaceId)
        {
            _data.SpaceId = spaceId;
            return this;
        }

        /// <summary>
        /// Override the NameKey property, or set it to null.
        /// </summary>
        public TeleportTableDataBuilder WithNameKey(string? nameKey)
        {
            _data.NameKey = nameKey;
            return this;
        }

        /// <summary>
        /// Add an additional locale entry to the first button's LocalizedName.
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
        /// Set LocalizedName to null to simulate missing data.
        /// </summary>
        public TeleportTableDataBuilder WithNullLocalizedName()
        {
            _data.LocalizedName = null;
            return this;
        }

        /// <summary>
        /// Set Buttons to null to simulate missing button collection.
        /// </summary>
        public TeleportTableDataBuilder WithNullButtons()
        {
            _data.Buttons = null;
            return this;
        }

        /// <summary>
        /// Return the configured TeleportTableData instance.
        /// </summary>
        public TeleportTableData Build() => _data;
    }
}
