// Repositories/Interfaces/IPredefinedCommentRepository.cs
using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories.Interfaces
{
    public interface IPredefinedCommentRepository
    {
        /// <summary>
        /// Returns predefined comments for a space, each with all i18n values available in that space.
        /// </summary>
        Task<List<PredefinedCommentModel>> GetPredefinedCommentsBySpaceAsync(int spaceId, bool includeInactive = false);

        /// <summary>
        /// Creates a predefined comment with a backend-generated comment_key and its i18n rows.
        /// </summary>
        Task<PredefinedCommentModel> CreatePredefinedCommentAsync(int spaceId, PredefinedCommentCreateDto dto);

        /// <summary>
        /// Updates is_active (optional) and upserts provided i18n values for the given predefined comment id.
        /// Returns null if the id doesn't exist.
        /// </summary>
        Task<PredefinedCommentModel?> UpdatePredefinedCommentByIdAsync(int spaceId, int id, PredefinedCommentUpdateDto dto);

        /// <summary>
        /// Soft-delete by default (sets is_active = FALSE). If hard=true, attempts a hard delete
        /// but returns ConflictInUse when referenced by booth_comment.
        /// </summary>
        Task<PredefinedCommentDeleteResult> DeletePredefinedCommentAsync(int spaceId, int id, bool hard = false, bool deleteI18nForSpace = false);
    }
}
