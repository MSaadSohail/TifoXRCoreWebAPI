
using TifoXRWebApi.Models.Common;

namespace TifoXRWebApi.Models
{
    public class SpaceData
    {
        public int Id { get; set; }
        public int PlatformTypeId { get; set; }
        public int EntityId { get; set; }
        public string Sku { get; set; }
        public string Link { get; set; }
        public bool IsPublished { get; set; }
        public bool IsLive { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime ModifiedTime { get; set; }
        public string ModifiedBy { get; set; }

        public LocalizedName LocalizedDescription { get; set; }
    }

    public class Space
    {
        public int PlatformTypeId { get; set; }
        public int EntityId { get; set; }
        public string Sku { get; set; }
        public string Link { get; set; }
        public bool IsPublished { get; set; }
        public bool IsLive { get; set; }
        public string ModifiedBy { get; set; }
        public int SpaceId { get; set; } // Used for i18n

        public LocalizedName LocalizedDescription { get; set; }
    }


}
