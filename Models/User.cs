// <copyright file="User.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Data for User </summary>
namespace GMS.TifoXRCoreWebAPI.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("user")]
    public class User
    {
        public string id { get; set; }

        
        public int? platform_type_id { get; set; }

        public string user_name { get; set; }

        
        public int user_type_id { get; set; }

        
        public int? user_age_range_id { get; set; }

        public DateTime creation_time { get; set; } = DateTime.UtcNow;
        // automatically handles TIMESTAMPs for optimistic concurrency
        public DateTime modified_time { get; set; } = DateTime.UtcNow;

        public string? modified_by { get; set; }
    }


}
