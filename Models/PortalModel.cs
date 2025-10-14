// <copyright file="PortalData.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>07/09/2025</date>
// <summary>Data for portals</summary>

using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Models
{
    /// <summary>
    /// DTO for updating a portal’s localized name (i18n), event and external link.
    /// </summary>
    public class PortalUpdateDto
    {
        public int? EventId { get; set; }
        public required LocalizedPairs LocalizedPairs { get; set; }
        public string? ExternalLink { get; set; }
        public MediaUpdateDto? CorrespondingMedia { get; set; }
        public MediaUpdateDto? ThumbnailMedia { get; set; }
    }

    /// <summary>
    /// Envelope for portal responses.
    /// </summary>
    public class PortalResponse
    {
        public required PortalModel Portal { get; set; }
    }

    /// <summary>
    /// Full portal payload including its localizations.
    /// </summary>
    public class PortalModel
    {
        public int Id { get; set; }
        public required int SpaceId { get; set; }
        public int? BoothId { get; set; }
        public int? PortalTypeId { get; set; }
        public int? EventId { get; set; }
        public required LocalizedPairs LocalizedPairs { get; set; }
        public string? ExternalLink { get; set; }
        public required MediaData CorrespondingMedia { get; set; }
        public required MediaData ThumbnailMedia { get; set; }
    }

    /// <summary>
    /// DTO for creating a new portal.
    /// </summary>
    public class PortalCreateDto
    {
        public int? BoothId { get; set; }
        public int? PortalTypeId { get; set; }
        public int? EventId { get; set; }
        public LocalizedPairs? LocalizedPairs { get; set; }
        public string? ExternalLink { get; set; }
        public MediaCreateDto? CorrespondingMedia { get; set; }
        public MediaCreateDto? ThumbnailMedia { get; set; }
    }
}
