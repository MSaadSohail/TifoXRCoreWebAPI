// <copyright file="PayPalOptions.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>09/02/2025</date>
// <summary></summary>

namespace GMS.TifoXRCoreWebAPI.Application.PaymentGateways.Paypal
{
    public sealed class PayPalOptions
    {
        public string ClientId { get; set; } = default!;
        public string Secret { get; set; } = default!;
        public string Environment { get; set; } = "Sandbox"; // or "Production"
    }
}
