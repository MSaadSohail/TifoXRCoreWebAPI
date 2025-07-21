
namespace TifoXRCoreWebAPI.Models
{
    /// <summary>
    /// DTO for upserting a media record.
    /// </summary>
    public class MediaUpdateDto
    {
        /// <summary>ID of the media row (omit or null to insert new).</summary>
        public string? Id { get; set; }
        public int MediaTypeId { get; set; }
        public string? TextKey { get; set; }
        public string? DescriptionKey { get; set; }
        public required List<MediaLocalization> Localizations { get; set; }
    }

    /// <summary>
    /// Full media payload including its localizations.
    /// </summary>
    public class MediaData
    {
        public required string Id { get; set; }
        public int MediaTypeId { get; set; }
        public string? TextKey { get; set; }
        public string? DescriptionKey { get; set; }
        public required List<MediaLocalization> Localizations { get; set; }
    }

    /// <summary>
    /// Single locale/link pair for a media item.
    /// </summary>
    public class MediaLocalization
    {
        public required string LocaleId { get; set; }
        public required string MediaLink { get; set; }
    }

    /// <summary>
    /// DTO for deleting media localizations of a portal.
    /// </summary>
    //public class MediaLocalizationDeleteDto
    //{
    //    /// <summary>Locale IDs to delete (e.g. ["en_us","es_es"])</summary>
    //    public required List<string> LocaleIds { get; set; }

    //    /// <summary>Whether to delete from the corresponding media.</summary>
    //    public bool DeleteCorresponding { get; set; }

    //    /// <summary>Whether to delete from the thumbnail media.</summary>
    //    public bool DeleteThumbnail { get; set; }
    //}

    public class MediaLocalizationDeleteDto
    {
        /// <summary>
        /// List of media entries (corresponding or thumbnail) and the locales to delete.
        /// </summary>
        public List<MediaLocales> Media { get; set; }
    }

    public class MediaLocales
    {
        /// <summary>ID of the media (must match this portal's corresponding or thumbnail media).</summary>
        public string MediaId { get; set; }
        /// <summary>Locale IDs to delete for this media.</summary>
        public List<string> LocaleIds { get; set; }
    }
}
