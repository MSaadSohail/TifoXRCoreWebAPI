
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Models
{
    public class BoothCreateDto
    {
        public int SpaceId { get; set; }
        public LocalizedName LocalizedName { get; set; }
    }

    public class BoothWrapper
    {
        public BoothModel booth { get; set; }
    }

    public class BoothModel
    {
        public int Id { get; set; }
        public int SpaceId { get; set; }
        public LocalizedName LocalizedName { get; set; }
    }

    public class BoothUpdateDto
    {
        public LocalizedName LocalizedName { get; set; }
    }

    public class Response
    {
        public BoothModel Booth { get; set; }
    }

    /// <summary>
    /// Client provides these fields.
    /// </summary>
    public class BoothActivity
    {
        public int BoothId { get; set; }
        public string UserId { get; set; }
        public int ExitCode { get; set; }
        public string BoothNameKey { get; set; }
        public string SessionId { get; set; }
        public DateTime EntryDatetime { get; set; }
        public int SessionDuration { get; set; }
        public DateTime? ExitDatetime { get; set; }
    }
}
