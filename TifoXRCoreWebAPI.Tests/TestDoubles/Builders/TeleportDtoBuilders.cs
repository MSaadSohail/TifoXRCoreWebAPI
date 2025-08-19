// © 2025 Global Mobile Software LLC. All rights reserved.
// Author: Urvashi Dhingra
// Date: 08/19/2025
// Summary:
// Fluent builders for TeleportTableCreateDto and ButtonCreateDto to keep
// POST tests concise and readable.

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace TifoXRCoreWebAPI.Tests.TestDoubles.Builders
{
    public sealed class TeleportTableCreateDtoBuilder
    {
        private string _nameKey = "teleport.table";
        private bool _isActive = true;
        private LocalizedPairs? _localizedPairs;
        private readonly List<ButtonCreateDto> _buttons = new();

        public static TeleportTableCreateDtoBuilder Default() => new TeleportTableCreateDtoBuilder();

        public TeleportTableCreateDtoBuilder WithNameKey(string key) { _nameKey = key; return this; }
        public TeleportTableCreateDtoBuilder WithIsActive(bool active) { _isActive = active; return this; }

        public TeleportTableCreateDtoBuilder WithTableLocales(params (string locale, string value)[] pairs)
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

        public TeleportTableCreateDtoBuilder AddButton(ButtonCreateDto button)
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

    public sealed class ButtonCreateDtoBuilder
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
