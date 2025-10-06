// <copyright file="StripeRefundPolicy.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/12/2025</date>
// <summary></summary>

namespace GMS.TifoXRCoreWebAPI.Application.Payments.Refunds
{
    /// <summary>
    /// Stripe supports a limited reason set: duplicate, fraudulent, requested_by_customer.
    /// </summary>
    public sealed class StripeRefundPolicy : IRefundPolicy
    {
        private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
        { "duplicate", "fraudulent", "requested_by_customer" };

        public bool IsAllowed(string? reason)
            => string.IsNullOrWhiteSpace(reason) || Allowed.Contains(reason.Trim());

        public string? Normalize(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) return null;
            var r = reason.Trim().ToLowerInvariant();
            return Allowed.Contains(r) ? r : null; // return null to signal “omit” if not allowed
        }
    }
}
