// <copyright file="SpaceDataBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/14/2025</date>
// <summary>
// Fluent builder for SpaceData used in unit tests to create reusable instances.
// </summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Builds SpaceData objects with sensible defaults and chainable overrides.
    /// </summary>
    public class SpaceDataBuilder
    {
        private int _id = 1;
        private int _platformTypeId = 1;
        private int _entityId = 1;
        private string? _sku = "SKU-001";
        private string? _link = "https://example.com/space/1";
        private bool _isPublished = true;
        private bool _isLive = true;
        private DateTime _creation = DateTime.UtcNow.AddDays(-1);
        private DateTime _modified = DateTime.UtcNow;
        private string _modifiedBy = "system";

        private LocalizedPairs _desc = new()
        {
            Key = "space_desc_key",
            Values = new List<LocalizedValue>
            {
                new() { LocaleId = "en_us", Value = "Default description" }
            }
        };

        /// <summary>Sets the SpaceData.Id.</summary>
        public SpaceDataBuilder WithId(int id) { _id = id; return this; }

        public SpaceDataBuilder WithPlatformTypeId(int value) { _platformTypeId = value; return this; }

        public SpaceDataBuilder WithEntityId(int value) { _entityId = value; return this; }

        /// <summary>Replaces the description localization with a single (locale,value) pair.</summary>
        public SpaceDataBuilder WithDescription(string locale, string? value)
        {
            _desc.Values = new List<LocalizedValue> { new() { LocaleId = locale, Value = value } };
            return this;
        }

        /// <summary>Copies fields from a Space DTO (SpaceId is DTO-only and ignored here).</summary>
        public SpaceDataBuilder FromDto(Space dto)
        {
            _platformTypeId = dto.PlatformTypeId;
            _entityId = dto.EntityId;
            _sku = dto.Sku;
            _link = dto.Link;
            _isPublished = dto.IsPublished;
            _isLive = dto.IsLive;
            _modifiedBy = dto.ModifiedBy ?? "system";
            _desc = dto.LocalizedDescription ?? new LocalizedPairs { Key = "space_desc_key", Values = new List<LocalizedValue>() };
            return this;
        }

        /// <summary>Builds a fresh SpaceData instance with the current settings.</summary>
        public SpaceData Build()
        {
            return new SpaceData
            {
                Id = _id,
                PlatformTypeId = _platformTypeId,
                EntityId = _entityId,
                Sku = _sku!,
                Link = _link!,
                IsPublished = _isPublished,
                IsLive = _isLive,
                CreationTime = _creation,
                ModifiedTime = _modified,
                ModifiedBy = _modifiedBy,
                LocalizedDescription = _desc
            };
        }
    }
}
