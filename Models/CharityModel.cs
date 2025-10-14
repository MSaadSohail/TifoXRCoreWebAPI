// Models/CharityModel.cs
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Models
{
    public sealed class CharityData
    {
        public int Id { get; init; }
        public int SpaceId { get; init; }                 // NEW: space-scoped
        public string? WebsiteUrl { get; init; }
        public string? DonationUrl { get; init; }
        public bool IsActive { get; init; }

        public LocalizedPairs LocalizedName { get; init; } = new() { Values = new List<LocalizedValue>() };
        public LocalizedPairs LocalizedDescription { get; init; } = new() { Values = new List<LocalizedValue>() };

        // Full media payload (logo)
        public MediaData? Media { get; init; }

        public DateTime CreationTime { get; init; }
        public DateTime ModifiedTime { get; init; }
        public string ModifiedBy { get; init; } = "system";
    }

    public sealed record CharityCreateDto
    {
        public int SpaceId { get; init; }
        public string? WebsiteUrl { get; init; }
        public string? DonationUrl { get; init; }
        public bool? IsActive { get; init; }
        public string? ModifiedBy { get; init; }
        public LocalizedPairs LocalizedName { get; init; } = new();
        public LocalizedPairs LocalizedDescription { get; init; } = new();
        public MediaUpdateDto? Media { get; init; }
    }

    public sealed class CharityUpdateDto
    {
        public int SpaceId { get; init; }                 // must match route spaceId
        public string? WebsiteUrl { get; init; }
        public string? DonationUrl { get; init; }
        public bool? IsActive { get; init; }
        public string? ModifiedBy { get; init; }

        // We ignore Key changes; only Values are upserted.
        public LocalizedPairs? LocalizedName { get; init; }
        public LocalizedPairs? LocalizedDescription { get; init; }

        // Optional: update media meta and/or its link localizations
        public MediaUpdateDto? Media { get; init; }
    }

}
