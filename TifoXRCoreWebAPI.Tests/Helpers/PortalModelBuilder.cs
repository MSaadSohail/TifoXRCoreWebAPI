// <copyright file="PortalModelBuilder.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>07/29/2025</date>
// <summary>Fluent builder for PortalModel used in tests to create customizable and reusable test instances.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TifoXRCoreWebAPI.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for PortalModel to reduce repetition and improve clarity in unit tests.
    /// Allows chaining of field customizations for PortalId, SpaceId, localization, media, etc.
    /// </summary>
    public class PortalModelBuilder
    {
        private readonly PortalModel _model = new()
        {
            PortalId = 1,
            SpaceId = 100,
            BoothId = 200,
            PortalTypeId = 1,
            EventId = 10,
            ExternalLink = "https://example.com",
            LocalizedName = new LocalizedResource
            {
                Key = "portal.name",
                Localizations = new Dictionary<string, string> { ["en_us"] = "Test Portal" }
            },
            CorrespondingMedia = new MediaData
            {
                Id = "m1",
                MediaTypeId = 1,
                LinkLocalizations = new Dictionary<string, string>()
            },
            ThumbnailMedia = new MediaData
            {
                Id = "m2",
                MediaTypeId = 2,
                LinkLocalizations = new Dictionary<string, string>()
            }
        };

        /// <summary>
        /// Sets the PortalId field.
        /// </summary>
        public PortalModelBuilder WithId(int id) 
        { 
            _model.PortalId = id; 
            return this; 
        }

        /// <summary>
        /// Sets the SpaceId field.
        /// </summary>
        public PortalModelBuilder WithSpaceId(int spaceId) 
        { 
            _model.SpaceId = spaceId; 
            return this; 
        }

        /// <summary>
        /// Sets the BoothId field.
        /// </summary>
        public PortalModelBuilder WithBoothId(int boothId) 
        {
            _model.BoothId = boothId; 
            return this; 
        }

        /// <summary>
        /// Sets the EventId field.
        /// </summary>
        public PortalModelBuilder WithEventId(int eventId) 
        { 
            _model.EventId = eventId; 
            return this; 
        }

        /// <summary>
        /// Sets the PortalTypeId field.
        /// </summary>
        public PortalModelBuilder WithPortalTypeId(int portalTypeId) 
        { 
            _model.PortalTypeId = portalTypeId; 
            return this; 
        }

        /// <summary>
        /// Sets the ExternalLink field.
        /// </summary>
        public PortalModelBuilder WithExternalLink(string link) 
        { 
            _model.ExternalLink = link; 
            return this; 
        }

        /// <summary>
        /// Sets the LocalizedName with a key and localized value.
        /// </summary>
        public PortalModelBuilder WithLocalizedName(string key, string value)
        {
            _model.LocalizedName = new LocalizedResource 
            { 
                Key = key, 
                Localizations = new Dictionary<string, string> 
                { 
                    { "en_us", value } 
                } 
            };
            return this;
        }

        /// <summary>
        /// Finalizes and returns the constructed PortalModel.
        /// </summary>
        public PortalModel Build() => _model;
    }
}
