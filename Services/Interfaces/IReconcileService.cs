// <copyright file="IReconcileService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>

namespace GMS.TifoXRCoreWebAPI.Services
{
    public interface IReconcileService
    {
        /// <summary>
        /// Scans pending/approved intents and attempts safe, idempotent capture and/or backfill.
        /// Returns the number of orders successfully finalized (charge+invoice ensured).
        /// </summary>
        Task<int> ReconcilePendingAsync(CancellationToken ct = default);
    }
}
