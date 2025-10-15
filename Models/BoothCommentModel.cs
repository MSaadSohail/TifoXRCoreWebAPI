// Models/BoothCommentCreateDto.cs
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Models
{
    public sealed class BoothCommentCreateDto
    {
        public string UserId { get; init; } = default!;
        public int PredefinedCommentId { get; init; }
        public bool IsActive { get; init; } = true; // publish by default
    }
    public sealed class BoothCommentModel
    {
        public int Id { get; set; }
        public int SpaceId { get; set; }
        public string UserId { get; set; } = default!;
        public int PredefinedCommentId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreationTime { get; set; }

        public string? UserName { get; set; }

        // Convenience for client display: the predefined comment's localized text (for this space)
        public LocalizedPairs LocalizedPairs { get; set; }
    }

    public sealed class BoothCommentUpdateDto
    {
        /// <summary>Optional: change the linked predefined comment.</summary>
        public int? PredefinedCommentId { get; init; }

        /// <summary>Optional: toggle active flag.</summary>
        public bool? IsActive { get; init; }
    }
}
