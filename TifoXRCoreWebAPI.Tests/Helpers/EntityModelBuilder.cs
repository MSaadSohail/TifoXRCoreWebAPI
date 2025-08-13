// <copyright file="EntityModelBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/12/2025</date>
// <summary>Fluent builder for EntityData used in unit tests to create reusable instances.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Builds EntityData objects with sensible defaults and chainable overrides.
    /// Mirrors the pattern used by Teleport builders for predictable test data.
    /// </summary>
    public class EntityModelBuilder
    {
        private int _id = 1;
        private int _entityTypeId = 1;
        private int? _parentEntityId = null;

        private LocalizedPairs _name = new()
        {
            Key = "entity_name_key",
            Values = new List<LocalizedValue>
            {
                new() { LocaleId = "en_us", Value = "Entity Name" }
            }
        };

        private LocalizedPairs _desc = new()
        {
            Key = "entity_desc_key",
            Values = new List<LocalizedValue>
            {
                new() { LocaleId = "en_us", Value = "Entity Description" }
            }
        };

        private DateTime _creation = DateTime.UtcNow.AddDays(-1);
        private DateTime _modified = DateTime.UtcNow;
        private string _modifiedBy = "system";

        /// <summary>Sets the EntityData.Id.</summary>
        public EntityModelBuilder WithId(int id) { _id = id; return this; }

        /// <summary>Sets the EntityData.EntityTypeId.</summary>
        public EntityModelBuilder WithEntityTypeId(int typeId) { _entityTypeId = typeId; return this; }

        /// <summary>Replaces the name localization with a single (locale,value) pair.</summary>
        public EntityModelBuilder WithName(string locale, string value)
        {
            _name.Values = new List<LocalizedValue> { new() { LocaleId = locale, Value = value } };
            return this;
        }

        /// <summary>Replaces the description localization with a single (locale,value) pair.</summary>
        public EntityModelBuilder WithDescription(string locale, string value)
        {
            _desc.Values = new List<LocalizedValue> { new() { LocaleId = locale, Value = value } };
            return this;
        }

        /// <summary>
        /// Copies fields from an Entity DTO (SpaceId is DTO-only and intentionally ignored).
        /// </summary>
        public EntityModelBuilder FromDto(Entity dto)
        {
            _entityTypeId = dto.EntityTypeId;
            _parentEntityId = dto.ParentEntityId;
            _name = dto.LocalizedPairs;
            _desc = dto.LocalizedDescription ?? new LocalizedPairs { Key = "", Values = new List<LocalizedValue>() };
            return this;
        }

        /// <summary>Builds a fresh EntityData instance with the current settings.</summary>
        public EntityData Build()
        {
            return new EntityData
            {
                Id = _id,
                EntityTypeId = _entityTypeId,
                ParentEntityId = _parentEntityId,
                LocalizedPairs = _name,
                LocalizedDescription = _desc,
                CreationTime = _creation,
                ModifiedTime = _modified,
                ModifiedBy = _modifiedBy
            };
        }
    }
}
