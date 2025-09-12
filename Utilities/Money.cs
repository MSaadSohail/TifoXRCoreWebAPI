// <copyright file="Money.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>

namespace GMS.TifoXRCoreWebAPI.Utilities
{
    /// <summary>
    /// Utility for safe conversion between minor (integers in DB) and major (decimals for gateways).
    /// </summary>
    public static class MoneyConverter
    {
        /// <summary>
        /// Convert minor units (long, e.g. cents) to major units (decimal, e.g. dollars).
        /// </summary>
        public static decimal ToMajor(long minor, int minorUnits = 100)
        {
            return minor / (decimal)minorUnits;
        }

        /// <summary>
        /// Convert major units (decimal, e.g. 10.50 USD) to minor (long, e.g. 1050).
        /// Rounds away from zero.
        /// </summary>
        public static long ToMinor(decimal major, int minorUnits = 100)
        {
            return (long)decimal.Round(major * minorUnits, 0, MidpointRounding.AwayFromZero);
        }
    }
}
