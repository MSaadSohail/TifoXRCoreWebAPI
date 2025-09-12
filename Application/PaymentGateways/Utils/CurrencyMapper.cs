// <copyright file="CurrencyMapper.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>

namespace GMS.TifoXRCoreWebAPI.Application.PaymentGateways.Utils
{
    public static class CurrencyMapper
    {
        // TODO: Wire to DB resolver; this keeps behavior identical to current hard-codes.
        public static string ResolveIso(int currencyId) => currencyId switch
        {
            2 => "USD",
            3 => "EUR",
            _ => "USD"
        };
    }
}

