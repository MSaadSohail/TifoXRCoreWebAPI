
using TifoXRWebApi.Models.Common;

namespace TifoXRWebApi.Models
{
    /// <summary>
    /// DTO for updating a portal’s localized name (i18n).
    /// </summary>
    public class PortalUpdateDto
    {
        public required LocalizedName LocalizedName { get; set; }

        /// <summary>Upsert this media against p.corresponding_media_id</summary>
        public MediaUpdateDto? CorrespondingMedia { get; set; }

        /// <summary>Upsert this media against p.thumbnail_media_id</summary>
        public MediaUpdateDto? ThumbnailMedia { get; set; }
    }

    /// <summary>
    /// Envelope for portal responses.
    /// </summary>
    public class PortalResponse
    {
        public required PortalData Portal { get; set; }
    }

    /// <summary>
    /// Full portal payload including its localizations.
    /// </summary>
    public class PortalData
    {
        public int PortalId { get; set; }
        public int SpaceId { get; set; }
        public int? BoothId { get; set; }
        public int? PortalTypeId { get; set; }
        public int? EventId { get; set; }
        public MediaData? CorrespondingMedia { get; set; }
        public MediaData? ThumbnailMedia { get; set; }
        public LocalizedName? TextFieldKey { get; set; }
        public string? ExternalLink { get; set; }
    }

    public class PortalCreateDto
    {
        public int? BoothId { get; set; }
        public int? PortalTypeId { get; set; }
        public int? EventId { get; set; }
        public LocalizedName?                                                                                                             LocalizedName { get; set; }
        public string? ExternalLink { get; set; }
        public MediaUpdateDto? CorrespondingMedia { get; set; }
        public MediaUpdateDto? ThumbnailMedia { get; set; }
    }
}
