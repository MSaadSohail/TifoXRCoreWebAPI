// <copyright file="SpaceModel.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Data for Space </summary>
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Models
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

        public LocalizedPair LocalizedDescription { get; set; }
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

        public LocalizedPair LocalizedDescription { get; set; }
    }


}
