// <copyright file="ICryptoGateway.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/11/2025</date>
// <summary></summary>

namespace GMS.TifoXRCoreWebAPI.Application.PaymentGateways
{
    public interface ICryptoGateway
    {
        Task<string> GetPreparedJsonAsync(string providerIntentId, string senderAddress);
        Task ReportTxAsync(string providerIntentId, string txHash);
    }
}
