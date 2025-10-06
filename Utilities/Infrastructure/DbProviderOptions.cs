// <copyright file="DbProviderOptions.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/02/2025</date>
// <summary></summary>

namespace GMS.TifoXRCoreWebAPI.Utilities.Infrastructure
{
    public sealed class DbProviderOptions
    {
        public string ProviderInvariantName { get; set; } = ""; // e.g., "MySqlConnector", "Microsoft.Data.SqlClient"
        public string ConnectionString { get; set; } = "";
    }
}
