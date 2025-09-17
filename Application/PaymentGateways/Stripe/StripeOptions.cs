// <copyright file="StripeOptions.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/9/2025</date>
// <summary></summary>

namespace GMS.TifoXRCoreWebAPI.Application.PaymentGateways.Stripe
{
    public sealed class StripeOptions
    {
        /// <summary>Secret key like "sk_live_..." or "sk_test_...".</summary>
        public string SecretKey { get; set; } = default!;

        /// <summary>
        /// Stripe API version override. Keep in sync with latest Basil minor.
        /// Example: "2025-08-27.basil"
        /// </summary>
        public string? ApiVersion { get; set; } = "2025-08-27.basil";

        /// <summary>Optional: webhook secret if you later add webhooks.</summary>
        public string? WebhookSecret { get; set; }
    }
}
