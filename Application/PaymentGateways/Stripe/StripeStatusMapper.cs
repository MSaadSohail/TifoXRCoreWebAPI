// <copyright file="StripeStatusMapper.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/12/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Utilities.Domain.Constants;

namespace GMS.TifoXRCoreWebAPI.Application.PaymentGateways.Stripe
{
    public static class StripeStatusMapper
    {
        public static string ToGeneric(string? piStatus) => piStatus switch
        {
            "requires_capture"          => GatewayStatus.Approved,
            "succeeded"                 => GatewayStatus.Succeeded,
            "processing"                => GatewayStatus.Processing,
            "requires_payment_method"   => GatewayStatus.RequiresAction,
            "requires_confirmation"     => GatewayStatus.RequiresAction,
            "canceled"                  => GatewayStatus.Canceled,
            _                           => piStatus?.ToUpperInvariant() ?? GatewayStatus.Pending
        };
    }
}
