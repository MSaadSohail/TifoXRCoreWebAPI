// <copyright file="EntityModel.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Data for Entity </summary>
using GMS.TifoXRCoreWebAPI.Models.Common;

namespace GMS.TifoXRCoreWebAPI.Models
{
    public class EntityData
    {
        public int Id { get; set; }
        public int EntityTypeId { get; set; }
        public int? ParentEntityId { get; set; }

        public LocalizedPairs LocalizedPairs { get; set; }
        public LocalizedPairs LocalizedDescription { get; set; }

        public DateTime CreationTime { get; set; }
        public DateTime ModifiedTime { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class Entity
    {
        public int SpaceId { get; set; }  // Required for i18n entries
        public int EntityTypeId { get; set; }
        public int? ParentEntityId { get; set; }

        public LocalizedPairs LocalizedPairs { get; set; }
        public LocalizedPairs LocalizedDescription { get; set; }
    }



}
