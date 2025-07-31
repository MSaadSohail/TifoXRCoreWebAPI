// <copyright file="TeleportTableDataBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>07/29/2025</date>
// <summary>Fluent builder for TeleportTableData used in unit tests to create customizable and reusable test instances.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using System.Collections.Generic;
namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for TeleportTableData to reduce repetition and improve clarity in unit tests.
    /// Allows chaining customizations for SpaceId, NameKey, localization, and button configuration.
    /// </summary>
    public class TeleportTableModelBuilder
    {
        private int _id = 1;
        private int _spaceId = 100;
        private string _nameKey = "table_key";
        private bool _isActive = true;
        private LocalizedPairs _localizedPairs = new LocalizedPairs
        {
            Key = "table_key",
            Values = new List<LocalizedValue>
            {
                new LocalizedValue { LocaleId = "en", Value = "Start" }
            }
        };
        private List<ButtonData> _buttons = new List<ButtonData>
        {
            new ButtonData
            {
                Id = 1,
                NameKey = "btn_key",
                IsActive = true,
                MapSpot = new MapSpotData { Id = 10, X = 0, Y = 0, Z = 0 },
                LocalizedPairs = new LocalizedPairs
                {
                    Key = "btn_key",
                    Values = new List<LocalizedValue>
                    {
                        new LocalizedValue { LocaleId = "en", Value = "Go" }
                    }
                }
            }
        };

        /// <summary>
        /// Sets the SpaceId for the TeleportTableData.
        /// </summary>
        public TeleportTableModelBuilder WithSpaceId(int spaceId)
        {
            _spaceId = spaceId;
            return this;
        }

        /// <summary>
        /// Sets the NameKey (and updates the table's LocalizedPairs.Key).
        /// </summary>
        public TeleportTableModelBuilder WithNameKey(string nameKey)
        {
            _nameKey = nameKey;
            if (_localizedPairs != null)
                _localizedPairs.Key = nameKey;
            return this;
        }

        /// <summary>
        /// Adds an extra locale entry to the first button’s LocalizedPairs.Values.
        /// </summary>
        public TeleportTableModelBuilder WithAdditionalButtonLocale(string locale, string text)
        {
            if (_buttons != null && _buttons.Count > 0)
            {
                var btn = _buttons[0];
                var pairs = btn.LocalizedPairs
                    ?? new LocalizedPairs { Key = btn.NameKey, Values = new List<LocalizedValue>() };
                pairs.Values.Add(new LocalizedValue { LocaleId = locale, Value = text });
                btn.LocalizedPairs = pairs;
            }
            return this;
        }

        /// <summary>
        /// Clears the table‐level localization.
        /// </summary>
        public TeleportTableModelBuilder WithNullLocalizedPairs()
        {
            _localizedPairs = null!;
            return this;
        }

        /// <summary>
        /// Clears the buttons collection.
        /// </summary>
        public TeleportTableModelBuilder WithNullButtons()
        {
            _buttons = null!;
            return this;
        }

        /// <summary>
        /// Builds a fresh TeleportTableData instance with the current settings.
        /// </summary>
        public TeleportTableData Build()
        {
            return new TeleportTableData
            {
                Id = _id,
                SpaceId = _spaceId,
                NameKey = _nameKey,
                IsActive = _isActive,
                LocalizedPairs = _localizedPairs,
                Buttons = _buttons
            };
        }
    }
}
