// <copyright file="PersonalityModel.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/25/2025</date>
// <summary>Personality Model for Personality Controller</summary>
using System;
using System.Collections.Generic;
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Models
{
    public class PersonalityData
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int? SportId { get; set; }
        public int? EntityId { get; set; }

        public LocalizedName LocalizedCountry { get; set; }
        public LocalizedName LocalizedBio { get; set; }

        public MediaData Media { get; set; }

        public DateTime CreationTime { get; set; }
        public DateTime ModifiedTime { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class PersonalityCreateDto
    {
        public string Name { get; set; }
        public int? SportId { get; set; }
        public int? EntityId { get; set; }
        public int SpaceId { get; set; } // Required for media
        public string? ModifiedBy { get; set; }

        public LocalizedName LocalizedCountry { get; set; } = new();
        public LocalizedName LocalizedBio { get; set; } = new();
        public MediaUpdateDto? Media { get; set; }
    }

    public class PersonalityUpdateDto
    {
        public string Name { get; set; }
        public int? SportId { get; set; }
        public int? EntityId { get; set; }
        public int SpaceId { get; set; } // Needed for media and i18n
        public string? ModifiedBy { get; set; }

        public LocalizedName LocalizedCountry { get; set; } = new();
        public LocalizedName LocalizedBio { get; set; } = new();

        public MediaUpdateDto? Media { get; set; }
    }




}
