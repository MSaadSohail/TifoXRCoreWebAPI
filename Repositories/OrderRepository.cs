// <copyright file="OrderRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/02/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    /// <summary>
    /// Data-access only. 
    /// No payment gateway calls here.
    /// </summary>
    public sealed partial class OrderRepository(IDbProvider db) : IOrderRepository
    {
        private readonly IDbProvider _db = db;
    }
}
