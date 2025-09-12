// <copyright file="IRefundPolicyResolver.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>

namespace GMS.TifoXRCoreWebAPI.Application.Payments.Refunds
{
    /// <summary>
    /// Maps a gateway to its refund policy. Keep gateway-name mapping here.
    /// </summary>
    public interface IRefundPolicyResolver
    {
        IRefundPolicy Resolve(string gatewayName);
    }

    public sealed class RefundPolicyResolver : IRefundPolicyResolver
    {
        private readonly IRefundPolicy _stripe = new StripeRefundPolicy();
        private readonly IRefundPolicy _noop = new NoopRefundPolicy();

        public IRefundPolicy Resolve(string gatewayName)
            => string.Equals(gatewayName, "stripe", StringComparison.OrdinalIgnoreCase)
                ? _stripe
                : _noop;
    }
}
