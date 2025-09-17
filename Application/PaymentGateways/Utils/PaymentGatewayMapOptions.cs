// <copyright file="PaymentGatewayMapOptions.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/9/2025</date>
// <summary></summary>

namespace TifoXRCoreWebAPI.Application.PaymentGateways.Utils
{
    public sealed class PaymentGatewayMapOptions
    {
        public Dictionary<int, string> IdToName { get; init; } = new();
    }
}

