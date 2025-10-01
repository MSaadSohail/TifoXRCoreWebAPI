// <copyright file="NoopRefundPolicy.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/12/2025</date>
// <summary></summary>

namespace GMS.TifoXRCoreWebAPI.Application.Payments.Refunds
{
    /// <summary>
    /// Default policy for gateways that don’t restrict reasons (or accept free text).
    /// Always allows; returns the input as-is (trimmed) or null if empty.
    /// </summary>
    public sealed class NoopRefundPolicy : IRefundPolicy
    {
        public bool IsAllowed(string? reason) => true;
        public string? Normalize(string? reason)
            => string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }
}
