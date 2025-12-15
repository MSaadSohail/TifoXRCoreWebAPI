// <copyright file="ShopModel.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>11/25/2025</date>
// <summary>DTOs for Shop read models.</summary>

using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Models
{
    public sealed class ShopDto
    {
        public int Id { get; init; }
        public int SpaceId { get; init; }
        public int PlatformTypeId { get; init; }
        public int? EntityId { get; init; }
        public string NameKey { get; init; } = default!;
        public DateTime CreationTime { get; init; }
        public DateTime ModifiedTime { get; init; }
        public string ModifiedBy { get; init; } = default!;
    }
    public sealed class CreateShopDto
    {
        public int PlatformTypeId { get; init; }
        public int? EntityId { get; init; }
        public string NameKey { get; init; } = default!;
        public LocalizedPairs? LocalizedPairs { get; init; }   
    }

    public sealed class UpdateShopDto
    {
        public int? PlatformTypeId { get; init; }

        public bool HasEntityId { get; init; } = false;
        public int? EntityId { get; init; }  // honored only if HasEntityId == true

        public string? NameKey { get; init; }

        /// <summary>Optional i18n upsert for the final NameKey.</summary>
        public LocalizedPairs? LocalizedPairs { get; init; }
    }
}
