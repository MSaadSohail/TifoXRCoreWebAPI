// <copyright file="CryptoChilizOptions.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/11/2025</date>
// <summary></summary>

namespace GMS.TifoXRCoreWebAPI.Application.PaymentGateways
{
    public sealed class CryptoChilizOptions
    {
        /// <summary>Thirdweb secret key (server-side).</summary>
        public string ThirdwebSecretKey { get; init; } = string.Empty;

        /// <summary>Your treasury wallet to receive CHZ.</summary>
        public string TreasuryAddress { get; init; } = string.Empty;

        /// <summary>Chiliz Spicy testnet = 88882 (mainnet: 88888).</summary>
        public int ChainId { get; init; } = 88882;

        public string RpcUrl { get; init; } = "https://spicy-rpc.chiliz.com";
        public int MinConfirmations { get; init; } = 1;

        /// <summary>Base URL of this API (used to build ApproveLink).</summary>
        public string PublicBaseUrl { get; init; } = "https://localhost:7017";
    }
}
