// <copyright file="TeleportDtoBuilders.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/19/2025</date>
// <summary> Fluent builders for TeleportTableCreateDto and ButtonCreateDto to keep
// POST tests concise and readable.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Tests.TestDoubles.Builders
{
    public class TeleportDtoBuilders
    {
        private string _nameKey = "teleport.table";
        private bool _isActive = true;
        private LocalizedPairs? _localizedPairs;
        private readonly List<ButtonCreateDto> _buttons = new();

        public static TeleportDtoBuilders Default() => new TeleportDtoBuilders();

        public TeleportDtoBuilders WithNameKey(string key) { _nameKey = key; return this; }
        public TeleportDtoBuilders WithIsActive(bool active) { _isActive = active; return this; }

        public TeleportDtoBuilders WithTableLocales(params (string locale, string value)[] pairs)
        {
            if (pairs is { Length: > 0 })
            {
                _localizedPairs = new LocalizedPairs
                {
                    Key = _nameKey,
                    Values = pairs.Select(p => new LocalizedValue { LocaleId = p.locale, Value = p.value }).ToList()
                };
            }
            return this;
        }

        public TeleportDtoBuilders AddButton(ButtonCreateDto button)
        {
            _buttons.Add(button);
            return this;
        }

        public TeleportTableCreateDto Build() => new TeleportTableCreateDto
        {
            NameKey = _nameKey,
            IsActive = _isActive,
            LocalizedPairs = _localizedPairs,
            Buttons = _buttons.Count == 0 ? null : _buttons
        };
    }

    public class ButtonCreateDtoBuilder
    {
        private string _nameKey = "btn.default";
        private bool _isActive = true;
        private MapSpotData? _mapSpot;
        private LocalizedPairs? _localizedPairs;

        public static ButtonCreateDtoBuilder Default() => new ButtonCreateDtoBuilder();

        public ButtonCreateDtoBuilder WithNameKey(string key) { _nameKey = key; return this; }
        public ButtonCreateDtoBuilder WithIsActive(bool active) { _isActive = active; return this; }

        public ButtonCreateDtoBuilder WithExistingMapSpotId(int id)
        {
            _mapSpot = new MapSpotData { Id = id, X = 0, Y = 0, Z = 0 };
            return this;
        }

        public ButtonCreateDtoBuilder WithNewMapSpot(decimal x, decimal y, decimal z)
        {
            _mapSpot = new MapSpotData { Id = 0, X = x, Y = y, Z = z };
            return this;
        }

        public ButtonCreateDtoBuilder WithButtonLocales(params (string locale, string value)[] pairs)
        {
            if (pairs is { Length: > 0 })
            {
                _localizedPairs = new LocalizedPairs
                {
                    Key = _nameKey,
                    Values = pairs.Select(p => new LocalizedValue { LocaleId = p.locale, Value = p.value }).ToList()
                };
            }
            return this;
        }

        public ButtonCreateDto Build() => new ButtonCreateDto
        {
            NameKey = _nameKey,
            IsActive = _isActive,
            MapSpot = _mapSpot,
            LocalizedPairs = _localizedPairs
        };
    }
}