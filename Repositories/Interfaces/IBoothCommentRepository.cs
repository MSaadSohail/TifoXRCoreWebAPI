// Repositories/Interfaces/IBoothCommentRepository.cs
using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface IBoothCommentRepository
    {
        /// <summary>
        /// Publishes a predefined comment for a user in a space.
        /// Idempotent per (spaceId, userId, predefined_comment_id).
        /// Throws InvalidOperationException if the predefined comment is inactive.
        /// </summary>
        Task<BoothCommentModel> CreateBoothCommentAsync(int spaceId, BoothCommentCreateDto dto);

        /// <summary>
        /// Returns booth_comment rows for a space. When includeInactive=false (default),
        /// only active booth_comment rows are returned.
        /// Each row includes localized values for its predefined comment key in that space.
        /// </summary>
        Task<List<BoothCommentModel>> GetBoothCommentsBySpaceAsync(int spaceId, bool includeInactive = false);

        /// <summary>
        /// Updates booth_comment by id (space-scoped). You may change predefined_comment_id and/or is_active.
        /// Throws InvalidOperationException for invalid inputs; DuplicateNameException for conflicts;
        /// returns null if the row (id, spaceId) is not found.
        /// </summary>
        Task<BoothCommentModel?> UpdateBoothCommentAsync(int spaceId, int id, BoothCommentUpdateDto dto);
    }
}

