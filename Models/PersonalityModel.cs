using System;
using System.Collections.Generic;
using TifoXRWebApi.Models.Common;

namespace TifoXRWebApi.Models
{
    public class PersonalityData
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int? SportId { get; set; }
        public int? EntityId { get; set; }

        public LocalizedName LocalizedCountry { get; set; }
        public LocalizedName LocalizedBio { get; set; }

        public MediaData Media { get; set; }

        public DateTime CreationTime { get; set; }
        public DateTime ModifiedTime { get; set; }
        public string ModifiedBy { get; set; }
    }
}
