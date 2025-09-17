// <copyright file="DbReaderExtensions.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/16/2025</date>
// <summary></summary>

using System.Data;

namespace GMS.TifoXRCoreWebAPI.Utilities.Infrastructure
{
    internal static class DbReaderExtensions
    {
        public static string? GetNullableString(this IDataRecord r, int ordinal)
            => r.IsDBNull(ordinal) ? null : r.GetString(ordinal);

        public static long? GetNullableInt64(this IDataRecord r, int ordinal)
            => r.IsDBNull(ordinal) ? (long?)null : r.GetInt64(ordinal);

        public static decimal? GetNullableDecimal(this IDataRecord r, int ordinal)
            => r.IsDBNull(ordinal) ? (decimal?)null : r.GetDecimal(ordinal);

        public static DateTime? GetNullableDateTime(this IDataRecord r, int ordinal)
            => r.IsDBNull(ordinal) ? (DateTime?)null : r.GetDateTime(ordinal);
    }
}

