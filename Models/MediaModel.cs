
namespace TifoXRWebApi.Models
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
        public List<MediaLocalization> Localizations { get; set; }
    }

    /// <summary>
    /// Full media payload including its localizations.
    /// </summary>
    public class MediaData
    {
        public string Id { get; set; }
        public int MediaTypeId { get; set; }
        public string? TextKey { get; set; }
        public string? DescriptionKey { get; set; }
        public List<MediaLocalization> Localizations { get; set; }
    }

    /// <summary>
    /// Single locale/link pair for a media item.
    /// </summary>
    public class MediaLocalization
    {
        public string LocaleId { get; set; }
        public string MediaLink { get; set; }
    }
}
