// <copyright file="IOrderRepository.Entitlements.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/16/2025</date>
// <summary></summary>

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public partial interface IOrderRepository
    {
        Task GrantEntitlementsAsync(string orderId);
    }
}

