// <copyright file="IPaymentGatewayResolver.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>08/02/2025</date>
// <summary>Interface to resolve names and ids of payment gateways</summary>

namespace GMS.TifoXRCoreWebAPI.Application.PaymentGateways
{
    public interface IPaymentGatewayResolver
    {
        IPaymentGateway GetById(int gatewayId);     // e.g., 1=stripe, 2=paypal, 3=wallet
        IPaymentGateway GetByName(string name);     // e.g., "stripe", "paypal", "crypto"
    }
}
