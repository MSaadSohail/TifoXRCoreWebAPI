// <copyright file="EntityDtoBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/12/2025</date>
// <summary>Fluent builder for Entity (DTO) to simplify controller test setup.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Builds Entity DTO objects with chainable overrides.
    /// Provides helpers to craft invalid DTOs for negative-path tests.
    /// </summary>
    public class EntityDtoBuilder
    {
        private int _spaceId = 1;
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

        /// <summary>Sets SpaceId (used by repository to stamp i18n rows).</summary>
        public EntityDtoBuilder WithSpaceId(int spaceId) { _spaceId = spaceId; return this; }

        /// <summary>Sets EntityTypeId.</summary>
        public EntityDtoBuilder WithEntityTypeId(int typeId) { _entityTypeId = typeId; return this; }

        /// <summary>Replaces the name localization with a single (locale,value) pair.</summary>
        public EntityDtoBuilder WithName(string locale, string value)
        {
            _name.Values = new List<LocalizedValue> { new() { LocaleId = locale, Value = value } };
            return this;
        }

        /// <summary>
        /// Makes LocalizedPairs.Values empty to trigger controller validation error.
        /// </summary>
        public EntityDtoBuilder WithEmptyNameValues()
        {
            _name.Values = new List<LocalizedValue>();
            return this;
        }

        /// <summary>Builds a fresh Entity DTO.</summary>
        public Entity Build()
        {
            return new Entity
            {
                SpaceId = _spaceId,
                EntityTypeId = _entityTypeId,
                ParentEntityId = _parentEntityId,
                LocalizedPairs = _name,
                LocalizedDescription = _desc
            };
        }
    }
}
