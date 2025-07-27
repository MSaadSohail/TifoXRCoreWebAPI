
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Models
{//Event
    public class EntityData
    {
        public int Id { get; set; }
        public int EntityTypeId { get; set; }
        public int? ParentEntityId { get; set; }

        public LocalizedName LocalizedName { get; set; }
        public LocalizedName LocalizedDescription { get; set; }

        public DateTime CreationTime { get; set; }
        public DateTime ModifiedTime { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class Entity
    {
        public int SpaceId { get; set; }  // Required for i18n entries
        public int EntityTypeId { get; set; }
        public int? ParentEntityId { get; set; }

        public LocalizedName LocalizedName { get; set; }
        public LocalizedName LocalizedDescription { get; set; }
    }



}
