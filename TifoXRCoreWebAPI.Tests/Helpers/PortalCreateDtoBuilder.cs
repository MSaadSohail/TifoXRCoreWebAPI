// <copyright file="PortalCreateDtoBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>07/29/2025</date>
// <summary>Fluent builder for PortalCreateDto used in unit tests to simplify setup of valid and edge-case DTOs.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for PortalCreateDto to simplify and standardize DTO creation in unit tests.
    /// Allows chaining of overrides for PortalTypeId, link, and localized name.
    /// </summary>
    public class PortalCreateDtoBuilder
    {
        private readonly PortalCreateDto _dto = new()
        {
            PortalTypeId = 1,
            ExternalLink = "https://example.com",
            LocalizedName = new LocalizedResource
            {
                Key = "portal.name",
                Localizations = new Dictionary<string, string> { ["en_us"] = "Test Portal" }
            }
        };

        /// <summary>
        /// Sets the PortalTypeId for the DTO.
        /// </summary>
        public PortalCreateDtoBuilder WithPortalTypeId(int id) 
        { 
            _dto.PortalTypeId = id; return this; 
        }

        /// <summary>
        /// Sets the ExternalLink for the DTO.
        /// </summary>
        public PortalCreateDtoBuilder WithLink(string link)
        {
            _dto.ExternalLink = link;
            return this;
        }

        /// <summary>
        /// Sets the localized name for the \"en_us\" locale.
        /// </summary>
        public PortalCreateDtoBuilder WithName(string name)
        {
            _dto.LocalizedName.Localizations["en_us"] = name;
            return this;
        }

        /// <summary>
        /// Sets the entire LocalizedName object for edge case or custom setup.
        /// </summary>
        public PortalCreateDtoBuilder WithLocalizedName(LocalizedResource? localizedName)
        {
            _dto.LocalizedName = localizedName;
            return this;
        }

        /// <summary>
        /// Finalizes and returns the configured PortalCreateDto instance.
        /// </summary>
        public PortalCreateDto Build() => _dto;
    }
}
