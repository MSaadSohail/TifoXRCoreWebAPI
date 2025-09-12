// <copyright file="CryptoChilizGateway.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/11/2025</date>
// <summary></summary>

using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Numerics;
using System.Text;
using System.Text.Json;
//
using Thirdweb;

namespace GMS.TifoXRCoreWebAPI.Application.PaymentGateways.Crypto.Chiliz
{
    /// <summary>
    /// Chiliz (CHZ) payments via thirdweb Bridge on Spicy testnet.
    /// Flow mirrors PayPal/Stripe: Create -> user approves/executes -> Capture.
    /// </summary>
    public sealed class CryptoChilizGateway : IPaymentGateway, ICryptoGateway
    {
        private readonly ThirdwebClient _client;
        private readonly CryptoChilizOptions _opts;
        private readonly ILogger<CryptoChilizGateway> _logger;
        private readonly IHttpClientFactory _http;

        public string Name => "crypto";

        // In-memory intent store; persist later if needed

        private static readonly ConcurrentDictionary<string, IntentState> _intents = new(
            StringComparer.OrdinalIgnoreCase
        );

        public CryptoChilizGateway(
            ThirdwebClient client,
            IOptions<CryptoChilizOptions> opts,
            ILogger<CryptoChilizGateway> logger,
            IHttpClientFactory http)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _opts = opts?.Value ?? throw new ArgumentNullException(nameof(opts));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        // inside CryptoChilizGateway
        private sealed record IntentState(
            int ChainId,
            string Receiver,
            BigInteger AmountWei,
            string? Sender,         // set when we prepare
            string? PreparedJson,   // cached prepared payload for that sender
            string Status,          // PENDING | APPROVED | SUCCEEDED | CANCELED
            string? TxHash
        );

        public Task<CreateGatewayIntentResult> CreateIntentAsync(CreateGatewayIntentRequest req)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.IdempotencyKey))
                throw new ArgumentException("IdempotencyKey is required.", nameof(req.IdempotencyKey));

            var wei = BigInteger.Parse(
                Thirdweb.Utils.ToWei(req.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture))
            );

            var providerIntentId = Guid.NewGuid().ToString("N");

            _intents[providerIntentId] = new IntentState(
                ChainId: _opts.ChainId,
                Receiver: _opts.TreasuryAddress,
                AmountWei: wei,
                Sender: null,
                PreparedJson: null,
                Status: "PENDING",
                TxHash: null
            );

            // Client must append ?sender=0xWallet when fetching prepared payload
            var approveLink = $"{_opts.PublicBaseUrl}/pay/crypto.html";
            return Task.FromResult(new CreateGatewayIntentResult(providerIntentId, approveLink));
        }

        public async Task<GatewayIntentStatusResult?> GetIntentAsync(string providerIntentId)
        {
            if (string.IsNullOrWhiteSpace(providerIntentId))
                throw new ArgumentException("providerIntentId is required.", nameof(providerIntentId));

            if (!_intents.TryGetValue(providerIntentId, out var s))
                return null;

            if (string.Equals(s.Status, "PENDING", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(s.TxHash))
                await TryVerifyAndApproveAsync(providerIntentId);

            var link = $"{_opts.PublicBaseUrl}/api/crypto/intents/{providerIntentId}";
            _intents.TryGetValue(providerIntentId, out s);
            return new GatewayIntentStatusResult(providerIntentId, s?.Status ?? "PENDING", link);
        }

        public async Task<CaptureGatewayResult> CaptureAsync(CaptureGatewayRequest req)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.IdempotencyKey))
                throw new ArgumentException("IdempotencyKey is required.", nameof(req.IdempotencyKey));
            if (string.IsNullOrWhiteSpace(req.ProviderIntentId))
                throw new ArgumentException("ProviderIntentId is required.", nameof(req.ProviderIntentId));

            if (!_intents.TryGetValue(req.ProviderIntentId, out var s))
                throw new InvalidOperationException("Unknown crypto provider intent id.");

            // Must be APPROVED (PaymentService enforces this too) :contentReference[oaicite:5]{index=5}
            // Must be APPROVED first (your PaymentService enforces this too)
            if (!string.Equals(s.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Crypto payment not approved yet.");

            if (string.IsNullOrWhiteSpace(s.TxHash))
                throw new InvalidOperationException("No transaction hash reported for this intent.");

            // If you want, call an external explorer/reader here later.
            // For now we trust the approval step and mark success.
            var amountMajor = WeiToDecimal(s.AmountWei, 18);

            // Mark succeeded
            s = s with { Status = "SUCCEEDED" };
            _intents[req.ProviderIntentId] = s;

            return new CaptureGatewayResult(
                ProviderChargeId: s.TxHash!,
                CapturedAmount: amountMajor,
                CurrencyIso: "CHZ"
            );

        }

        private static decimal WeiToDecimal(BigInteger wei, int decimals)
        {
            // Avoid overflow by doing decimal division with a decimal constant
            var divisor = decimals switch
            {
                18 => 1_000_000_000_000_000_000m,
                6 => 1_000_000m,
                _ => (decimal)Math.Pow(10, decimals)
            };

            // NOTE: BigInteger -> decimal cast can overflow for extremely large values.
            // For payments we expect amounts well within decimal range.
            return (decimal)wei / divisor;
        }

        public Task<RefundGatewayResult> RefundAsync(RefundGatewayRequest req)
        {
            // Optional: implement treasury->user CHZ transfer prepare/execute + return tx hash.
            throw new NotSupportedException("Crypto refunds are not yet supported.");
        }

        // ---------- Helpers used by controller endpoints ----------


        public async Task ReportTxAsync(string pid, string txHash)
        {
            if (!_intents.TryGetValue(pid, out var s))
                throw new InvalidOperationException("Unknown provider intent id.");

            if (!txHash.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || txHash.Length < 66)
                throw new ArgumentException("Invalid transaction hash.", nameof(txHash));

            s = s with { TxHash = txHash };
            _intents[pid] = s;

            // Try to verify now; if not enough confirmations yet, we'll remain PENDING.
            var ok = await TryVerifyAndApproveAsync(pid);
            _logger.LogInformation("Crypto intent {Pid} verification after report: {Ok}", pid, ok);
        }

        // in CryptoChilizGateway.cs
        public async Task<string> GetPreparedJsonAsync(string pid, string senderAddress)
        {
            if (!_intents.TryGetValue(pid, out var s))
                throw new InvalidOperationException("Unknown provider intent id.");

            // Basic validations (fail fast with 400s instead of 500s)
            if (string.IsNullOrWhiteSpace(senderAddress) || senderAddress.Length != 42 || !senderAddress.StartsWith("0x"))
                throw new ArgumentException("sender must be a valid EVM address", nameof(senderAddress));
            if (string.IsNullOrWhiteSpace(_opts.TreasuryAddress) || _opts.TreasuryAddress.Length != 42 || !_opts.TreasuryAddress.StartsWith("0x"))
                throw new ArgumentException("TreasuryAddress is invalid in configuration.");
            if (_opts.ChainId != 88882 && _opts.ChainId != 88888)
                throw new ArgumentException($"Unsupported ChainId '{_opts.ChainId}'. Use 88882 (Spicy) or 88888 (Mainnet).");

            // Cache hit for same sender
            if (s.Sender is not null && s.PreparedJson is not null &&
                string.Equals(s.Sender, senderAddress, StringComparison.OrdinalIgnoreCase))
                return s.PreparedJson;

            // Try thirdweb prepare; if it fails, fall back to a simple native transfer
            try
            {
                if (string.IsNullOrWhiteSpace(_opts.ThirdwebSecretKey))
                    throw new InvalidOperationException("ThirdwebSecretKey is missing.");

                var bridge = await Thirdweb.Bridge.ThirdwebBridge.Create(_client);
                var prepared = await bridge.Transfer_Prepare(
                    chainId: s.ChainId,
                    tokenAddress: Constants.NATIVE_TOKEN_ADDRESS,
                    transferAmountWei: s.AmountWei,
                    sender: senderAddress,
                    receiver: s.Receiver
                );

                var json = JsonSerializer.Serialize(prepared);
                _intents[pid] = s with { Sender = senderAddress, PreparedJson = json };
                return json;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "thirdweb prepare failed; falling back to raw native tx for {Pid}", pid);

                // Fallback: direct native CHZ tx (MetaMask will fill gas)
                var fallback = new[]
                {
            new {
                to = s.Receiver,
                data = "0x",
                value = "0x" + s.AmountWei.ToString("X") // hex wei
            }
        };

                var json = JsonSerializer.Serialize(fallback);
                _intents[pid] = s with { Sender = senderAddress, PreparedJson = json };
                return json;
            }
        }

        // ---- JSON-RPC shapes ----
        private sealed record RpcTx(string? hash, string? from, string? to, string? value, string? blockNumber);
        private sealed record RpcReceipt(string? transactionHash, string? status, string? blockNumber);

        // Returns true if APPROVED after check
        private async Task<bool> TryVerifyAndApproveAsync(string pid)
        {
            if (!_intents.TryGetValue(pid, out var s) || string.IsNullOrWhiteSpace(s.TxHash))
                return false;

            try
            {
                // 1) Fetch tx + receipt
                var tx = await RpcCallAsync<RpcTx>("eth_getTransactionByHash", s.TxHash!);
                var receipt = await RpcCallAsync<RpcReceipt>("eth_getTransactionReceipt", s.TxHash!);
                if (tx is null || receipt is null) return false;

                // 2) Must be mined and successful
                if (string.IsNullOrWhiteSpace(tx.blockNumber) || string.IsNullOrWhiteSpace(receipt.blockNumber))
                    return false; // pending
                if (!IsSuccessStatus(receipt.status))
                    return false; // failed

                // 3) Confirmations
                var headHex = await RpcCallAsync<string>("eth_blockNumber");
                if (string.IsNullOrWhiteSpace(headHex)) return false;
                var head = HexToBigInt(headHex);
                var mined = HexToBigInt(receipt.blockNumber!);
                var confirmations = head - mined;
                if (confirmations < _opts.MinConfirmations)
                    return false; // not enough confs yet

                // 4) Validate destination and amount (native CHZ)
                if (!AddressEqual(tx.to, _opts.TreasuryAddress))
                    return false;
                if (HexToBigInt(tx.value ?? "0x0") < s.AmountWei)
                    return false;

                // 5) All good → APPROVED
                s = s with { Status = "APPROVED" };
                _intents[pid] = s;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RPC verification failed for {Pid}", pid);
                return false;
            }
        }
        // inside CryptoChilizGateway class
        internal bool TryGetNativeParams(string pid, out string receiver, out string valueHex, out int chainId)
        {
            receiver = default!;
            valueHex = default!;
            chainId = _opts.ChainId;

            if (!_intents.TryGetValue(pid, out var s))
                return false;

            receiver = s.Receiver;
            valueHex = "0x" + s.AmountWei.ToString("X"); // hex wei
            return true;
        }

        private async Task<T?> RpcCallAsync<T>(string method, params object[] parameters)
        {
            var client = _http.CreateClient();
            var payload = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = 1,
                method,
                @params = parameters
            });
            using var req = new HttpRequestMessage(HttpMethod.Post, _opts.RpcUrl)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            using var res = await client.SendAsync(req);
            res.EnsureSuccessStatusCode();
            using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
            if (doc.RootElement.TryGetProperty("error", out var err))
                throw new InvalidOperationException($"RPC error: {err}");

            if (!doc.RootElement.TryGetProperty("result", out var result) || result.ValueKind == JsonValueKind.Null)
                return default;

            return result.Deserialize<T>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private static bool AddressEqual(string? a, string? b)
            => !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b)
               && string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

        private static bool IsSuccessStatus(string? statusHex)
            => string.Equals(statusHex, "0x1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(statusHex, "0x01", StringComparison.OrdinalIgnoreCase);

        private static BigInteger HexToBigInt(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return BigInteger.Zero;
            var s = hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hex[2..] : hex;
            return BigInteger.Parse("0" + s, System.Globalization.NumberStyles.AllowHexSpecifier);
        }

    }
}
