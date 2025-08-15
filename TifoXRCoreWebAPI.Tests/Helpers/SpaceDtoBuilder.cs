// © 2025 Global Mobile Software LLC. All Rights Reserved.
// <author>Urvashi Dhingra</author>
// <date>08/14/2025</date>
// <summary>
// Fluent builder for Space (DTO) used in controller tests to simplify setup.
// </summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Builds Space DTO objects with chainable overrides.
    /// Provides helpers to craft invalid DTOs for negative-path tests.
    /// </summary>
    public class SpaceDtoBuilder
    {
        private int _platformTypeId = 1;
        private int _entityId = 1;
        private string? _sku = "SKU-001";
        private string? _link = "https://example.com/space";
        private bool _isPublished = true;
        private bool _isLive = true;
        private string _modifiedBy = "system";
        private int _spaceId = 1; // used by repository for i18n updates

        private LocalizedPairs _desc = new()
        {
            Key = "space_desc_key",
            Values = new List<LocalizedValue>
            {
                new() { LocaleId = "en_us", Value = "Default description" }
            }
        };

        public SpaceDtoBuilder WithPlatformTypeId(int value) { _platformTypeId = value; return this; }

        public SpaceDtoBuilder WithEntityId(int value) { _entityId = value; return this; }

        public SpaceDtoBuilder WithSku(string? value) { _sku = value; return this; }

        public SpaceDtoBuilder WithLink(string? value) { _link = value; return this; }

        /// <summary>Replace the description localization with a single (locale,value) pair.</summary>
        public SpaceDtoBuilder WithDescription(string locale, string? value)
        {
            _desc.Values = new List<LocalizedValue> { new() { LocaleId = locale, Value = value } };
            return this;
        }

        /// <summary>Make LocalizedDescription.Values null to trigger controller validation.</summary>
        public SpaceDtoBuilder WithNullDescriptionValues()
        {
            _desc.Values = null!;
            return this;
        }

        /// <summary>Make LocalizedDescription.Values empty to trigger controller validation.</summary>
        public SpaceDtoBuilder WithEmptyDescriptionValues()
        {
            _desc.Values = new List<LocalizedValue>();
            return this;
        }

        /// <summary>Builds a fresh Space DTO.</summary>
        public Space Build()
        {
            return new Space
            {
                PlatformTypeId = _platformTypeId,
                EntityId = _entityId,
                Sku = _sku!,
                Link = _link!,
                IsPublished = _isPublished,
                IsLive = _isLive,
                ModifiedBy = _modifiedBy,
                SpaceId = _spaceId,
                LocalizedDescription = _desc
            };
        }
    }
}
