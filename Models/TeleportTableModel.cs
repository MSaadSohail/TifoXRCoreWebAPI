// <copyright file="TeleportTableModel.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/09/2025</date>
// <summary>Data for teleport table</summary>

using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Models
{
    public class TeleportTableData
    {
        public int Id { get; set; }
        public int SpaceId { get; set; }
        public string? NameKey { get; set; }
        public bool IsActive { get; set; }
        public LocalizedName? LocalizedName { get; set; }
        public List<ButtonData>? Buttons { get; set; }
    }

    public class ButtonData
    {
        public int Id { get; set; }
        public string NameKey { get; set; }
        public bool IsActive { get; set; }
        public LocalizedName? LocalizedName { get; set; }
        public MapSpotData MapSpot { get; set; } = default!;
    }

    public class TeleportTableCreateDto
    {
        public int SpaceId { get; set; }
        public string? NameKey { get; set; }
        public bool IsActive { get; set; }
        public LocalizedName? LocalizedName { get; set; }
        public List<ButtonCreateDto>? Buttons { get; set; }
    }

    public class TeleportTableUpdateDto
    {
        public bool IsActive { get; set; }
        public string? NameKey { get; set; }
        public LocalizedName? LocalizedName { get; set; }
        public List<ButtonUpdateDto>? Buttons { get; set; }
    }

    public class ButtonCreateDto
    {
        public string NameKey { get; set; } = default!;
        public MapSpotData MapSpot { get; set; } = default!;
        public LocalizedName? LocalizedName { get; set; }
        public bool IsActive { get; set; }
    }

    public class ButtonUpdateDto
    {
        public int? Id { get; set; }
        public string NameKey { get; set; } = default!;
        public MapSpotData MapSpot { get; set; } = default!;
        public LocalizedName? LocalizedName { get; set; }
        public bool IsActive { get; set; }
    }

    public class TeleportTableResponse
    {
        public TeleportTableData TeleportTable { get; set; } = default!;
    }

    public class MapSpotData
    {
        public int Id { get; set; }
        public decimal X { get; set; }
        public decimal Y { get; set; }
        public decimal Z { get; set; }
    }
}
