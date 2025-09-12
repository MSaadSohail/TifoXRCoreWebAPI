// <copyright file="PayPalStatusMapper.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>


// <copyright file="PayPalStatusMapper.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Utilities.Domain.Constants;

namespace GMS.TifoXRCoreWebAPI.Application.PaymentGateways.Paypal
{
    public static class PayPalStatusMapper
    {
        public static string ToGeneric(string? paypalStatus) => paypalStatus?.ToUpperInvariant() switch
        {
            "APPROVED"              => GatewayStatus.Approved,
            "COMPLETED"             => GatewayStatus.Succeeded, // after capture
            "PAYER_ACTION_REQUIRED" => GatewayStatus.RequiresAction,
            "CREATED" or "SAVED"    => GatewayStatus.RequiresAction,
            "VOIDED" or "CANCELLED" => GatewayStatus.Canceled,
            _                       => GatewayStatus.Pending
        };
    }
}
