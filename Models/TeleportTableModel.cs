using TifoXRWebApi.Models.Common;

namespace TifoXRWebApi.Models
{
    /// <summary>
    /// The data returned for each teleport table.
    /// </summary>
    public class TeleportTableData
    {
        public int Id { get; set; }
        public int SpaceId { get; set; }
        public bool IsActive { get; set; }
        public LocalizedName LocalizedName { get; set; }
        public List<ButtonData> Buttons { get; set; }
    }

    public class ButtonData
    {
        public int Id { get; set; }
        public LocalizedName LocalizedName { get; set; }
    }

    /// <summary>
    /// DTO used to create a new teleport table.
    /// </summary>
    public class TeleportTableCreateDto
    {
        public bool IsActive { get; set; }
        public LocalizedName LocalizedName { get; set; }
    }

    public class TeleportTableUpdateDto
    {
        public bool IsActive { get; set; }
        public LocalizedName LocalizedName { get; set; }
        public List<ButtonUpdateDto>? Buttons { get; set; }
    }

    public class ButtonUpdateDto
    {
        /// <summary>
        /// The DB identity for an existing button; null ⇒ insert new.
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// The i18n key + its localized values for the button text.
        /// </summary>
        public LocalizedName LocalizedName { get; set; }
    }

    /// <summary>
    /// Wrapper for create/update responses.
    /// </summary>
    public class TeleportTableResponse
    {
        public TeleportTableData TeleportTable { get; set; }
    }
}
