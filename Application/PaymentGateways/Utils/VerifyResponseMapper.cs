// <copyright file="VerifyResponseMapper.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Application.PaymentGateways.Utils
{
    public static class VerifyResponseMapper
    {
        public static VerifyPaymentResponse Build(OrderDto dto)
        {
            var capturedMinor = dto.PaymentIntents.SelectMany(i => i.Charges).Sum(c => c.AmountCapturedMinor);
            var isPaid = capturedMinor >= dto.TotalNetAmount;
            var hasEntitlements = dto.Entitlements?.Any(e => e.GrantedDateTime != null) == true;
            var hasInvoice = dto.Invoices?.Any() == true;

            var missing = new System.Collections.Generic.List<string>();
            if (!isPaid) missing.Add("charge");
            if (isPaid && !hasEntitlements) missing.Add("entitlements");
            if (isPaid && !hasInvoice) missing.Add("invoice");

            return new VerifyPaymentResponse
            {
                OrderId = dto.Id,
                IsPaid = isPaid,
                CapturedMinor = capturedMinor,
                TotalNetMinor = dto.TotalNetAmount,
                HasEntitlements = hasEntitlements,
                HasInvoice = hasInvoice,
                Missing = missing
            };
        }
    }
}
