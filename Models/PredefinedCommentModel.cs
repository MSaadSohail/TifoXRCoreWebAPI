// Models/PredefinedCommentModel.cs
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Models
{
    public sealed class PredefinedCommentModel
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreationTime { get; set; }

        // Same structure you already use elsewhere
        public LocalizedPairs LocalizedPairs { get; set; }
    }

    public sealed class PredefinedCommentCreateDto
    {
        /// <summary>
        /// Defaults to true. Stored in predefined_comments.is_active (BIT(1) → bool).
        /// </summary>
        public bool IsActive { get; init; } = true;

        /// <summary>
        /// Localized values to create for this comment in the given space.
        /// Only supported locales (from supported_languages) will be inserted.
        /// </summary>
        public List<LocalizedValue>? Localizations { get; init; }
        // LocalizedValue: { string LocaleId, string? Value }
    }

    public sealed class PredefinedCommentUpdateDto
    {
        /// <summary>
        /// If omitted, is_active remains unchanged.
        /// </summary>
        public bool? IsActive { get; init; }

        /// <summary>
        /// Locales to upsert. Only Values are used; Key is ignored on update.
        /// Unspecified locales remain unchanged.
        /// </summary>
        public LocalizedPairs? LocalizedPairs { get; init; }
    }

    public sealed class PredefinedCommentDeleteResult
    {
        public bool NotFound { get; init; }
        public bool ConflictInUse { get; init; }
        public bool HardDeleted { get; init; }
        public bool SoftDeleted { get; init; }
    }
}
