// <copyright file="TeleportTableModel.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/09/2025</date>
// <summary>Data for teleport table</summary>

namespace TifoXRCoreWebAPI.Models
{
    /// <summary>
    /// The data returned for each teleport table.
    /// </summary>
    public class TeleportTableData
    {
        public int Id { get; set; }
        public int SpaceId { get; set; }
        public string? NameKey { get; set; }
        public bool IsActive { get; set; }
        public Dictionary<string,string>? LocalizedName { get; set; }
        public List<ButtonData>? Buttons { get; set; }
    }

    public class ButtonData
    {
        public int Id { get; set; }
        public string NameKey { get; set; }
        public int BoothToVisit { get; set; }
        public Dictionary<string, string> LocalizedName { get; set; }
    }

    /// <summary>
    /// DTO used to create a new teleport table.
    /// </summary>
    public class TeleportTableCreateDto
    {
        public int SpaceId { get; set; } // Required for table creation
        public string? NameKey { get; set; } // i18n key for the table's name (optional if handled by i18n service)
        public bool IsActive { get; set; }
        public Dictionary<string, string>? LocalizedName { get; set; } // Localized name values (optional, can be ignored if service-side i18n)
        public List<ButtonCreateDto>? Buttons { get; set; } // Buttons to create with the table
    }

    public class TeleportTableUpdateDto
    {
        public bool IsActive { get; set; }
        public string? NameKey { get; set; }
        public Dictionary<string, string>? LocalizedName { get; set; }
        public List<ButtonUpdateDto>? Buttons { get; set; }
    }

    public class ButtonCreateDto
    {
        public string NameKey { get; set; } // i18n key for the button's name
        public Dictionary<string, string>? LocalizedName { get; set; } // Localized name values (optional)
        public int BoothToVisit { get; set; } // FK to booth to teleport to
    }

    public class ButtonUpdateDto
    {
        public int? Id { get; set; }
        public string NameKey { get; set; } // i18n key for the button's name
        public Dictionary<string, string>? LocalizedName { get; set; }
        public int BoothToVisit { get; set; }
    }

    /// <summary>
    /// Wrapper for create/update responses.
    /// </summary>
    public class TeleportTableResponse
    {
        public TeleportTableData TeleportTable { get; set; }
    }

}
