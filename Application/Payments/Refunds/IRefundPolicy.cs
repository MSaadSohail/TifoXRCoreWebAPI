// <copyright file="IRefundPolicy.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>

namespace GMS.TifoXRCoreWebAPI.Application.Payments.Refunds
{
    /// <summary>
    /// Encapsulates provider-specific refund validation/normalization.
    /// </summary>
    public interface IRefundPolicy
    {
        /// <summary>Returns true if the given reason is allowed for this provider.</summary>
        bool IsAllowed(string? reason);

        /// <summary>
        /// Returns a normalized provider-facing reason value (or null if reason should be omitted).
        /// Implementations should trim and case-normalize.
        /// </summary>
        string? Normalize(string? reason);
    }
}
