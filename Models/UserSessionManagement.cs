// <copyright file="UserSessionManagement.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Data for User Session </summary>
namespace GMS.TifoXRCoreWebAPI.Models
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("user_session_management")]
    public class UserSessionManagement
    {

        public string id { get; set; }

        public string user_id { get; set; }

        public string? geolocation_id { get; set; }
        public int platform_type_id { get; set; }

        public int space_id { get; set; }

        public string? ip_address { get; set; }

        public string? oauth_token { get; set; }

        public int status_id { get; set; }

        public DateTime start_time { get; set; } = DateTime.UtcNow;

        public DateTime? last_active_time { get; set; }

        public DateTime end_time { get; set; } = DateTime.UtcNow.AddHours(1);

        public DateTime expiration_time { get; set; } = DateTime.UtcNow.AddHours(1);

        public DateTime creation_time { get; set; } = DateTime.UtcNow;

        public DateTime modified_time { get; set; } = DateTime.UtcNow;
        public string? modified_by { get; set; }
    }


}
