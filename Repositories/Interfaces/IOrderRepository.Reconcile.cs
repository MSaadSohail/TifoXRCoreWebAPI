// <copyright file="IOrderRepository.Reconcile.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public partial interface IOrderRepository
    {
        Task<IReadOnlyList<ReconcileCandidate>> FindOrdersNeedingReconcileAsync(CancellationToken ct);
    }
}
