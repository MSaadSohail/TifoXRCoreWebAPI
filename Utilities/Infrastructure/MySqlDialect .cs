// <copyright file="MySqlDialect .cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/02/2025</date>
// <summary></summary>


namespace GMS.TifoXRCoreWebAPI.Utilities.Infrastructure
{
    public sealed class MySqlDialect : ISqlDialect
    {
        public string AppendIdentitySelect(string insertSql, string idColumn = "id")
            => $"{insertSql}; SELECT LAST_INSERT_ID();";
    }
}
