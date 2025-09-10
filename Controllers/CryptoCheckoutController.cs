using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using GMS.TifoXRCoreWebAPI.Application.PaymentGateways;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    [Route("pay/crypto")]
    public sealed class CryptoCheckoutController : ControllerBase
    {
        private readonly CryptoChilizOptions _opts;
        private readonly CryptoChilizGateway _crypto; // NEW

        public CryptoCheckoutController(
            IOptions<CryptoChilizOptions> opts,
            IEnumerable<IPaymentGateway> gateways)     // NEW
        {
            _opts = opts.Value;
            _crypto = gateways.OfType<CryptoChilizGateway>().First()
                      ?? throw new InvalidOperationException("CryptoChilizGateway not registered.");
        }

        // GET /pay/crypto/execute?pid=...&orderId=...&intentId=...&spaceId=1
        [HttpGet("execute")]
        public IActionResult Execute([FromQuery] string pid, [FromQuery] string orderId, [FromQuery] string intentId, [FromQuery] string? spaceId = "1")
        {
            if (string.IsNullOrWhiteSpace(pid) || string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(intentId))
                return BadRequest("pid, orderId, and intentId are required.");

            if (!_crypto.TryGetNativeParams(pid, out var to, out var valueHex, out var chainId))
                return NotFound("Intent not found or expired. Create a new payment intent.");

            var pidEsc = WebUtility.HtmlEncode(pid);
            var orderEsc = WebUtility.HtmlEncode(orderId);
            var intentEsc = WebUtility.HtmlEncode(intentId);
            var spaceEsc = WebUtility.HtmlEncode(spaceId ?? "1");
            var toEsc = WebUtility.HtmlEncode(to);
            var valEsc = WebUtility.HtmlEncode(valueHex);
            var chainHex = "0x" + chainId.ToString("X"); // e.g., 0x15B32 for 88882

            var html = $@"<!doctype html>
<html>
<head>
  <meta charset=""utf-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
  <title>TifoXR Crypto Checkout</title>
  <style>
    body {{ font-family: system-ui, -apple-system, Segoe UI, Roboto, sans-serif; padding: 24px; }}
    .card {{ max-width: 560px; margin: 0 auto; padding: 20px; border: 1px solid #e5e7eb; border-radius: 12px; }}
    button {{ padding: 10px 16px; border-radius: 10px; border: 0; cursor: pointer; }}
    #log {{ margin-top: 16px; background:#0b1020; color:#d0d6ff; padding:12px; border-radius:8px; white-space:pre-wrap; font-size: 12px; }}
  </style>
</head>
<body>
  <div class=""card"">
    <h2>Pay with MetaMask (Chiliz)</h2>
    <p>Click the button to open MetaMask and complete the payment.</p>
    <div style=""display:flex; gap:12px; margin-top:8px;"">
      <button id=""connect"">Connect Wallet</button>
      <button id=""pay"" disabled>Pay</button>
    </div>
    <div id=""log""></div>
  </div>

<script>
const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms));
const PID        = ""{pidEsc}"";
const ORDER_ID   = ""{orderEsc}"";
const INTENT_ID  = ""{intentEsc}"";
const SPACE_ID   = ""{spaceEsc}"";
// No need to call any prepare endpoint:
const TO         = ""{toEsc}"";      // Treasury
const VALUE_HEX  = ""{valEsc}"";     // Amount in wei (hex), computed server-side
const CHAIN_ID_HEX = ""{chainHex}""; // From your options (e.g., 0x15B32 for Spicy)

const API_BASE = window.location.origin; // always same origin
const SPICY = {{
  chainId: CHAIN_ID_HEX,
  chainName: 'Chiliz Spicy Testnet',
  nativeCurrency: {{ name: 'Chiliz', symbol: 'CHZ', decimals: 18 }},
  rpcUrls: ['https://spicy-rpc.chiliz.com/'],
  blockExplorerUrls: ['https://testnet.chiliscan.com/']
}};

let account = null;

function log(msg) {{
  const el = document.getElementById('log');
  el.textContent += msg + '\\n';
}}

async function ensureChain() {{
  try {{
    await ethereum.request({{ method: 'wallet_switchEthereumChain', params: [{{ chainId: CHAIN_ID_HEX }}] }});
  }} catch (err) {{
    if (err && err.code === 4902) {{
      await ethereum.request({{ method: 'wallet_addEthereumChain', params: [SPICY] }});
    }} else {{
      throw err;
    }}
  }}
}}

document.getElementById('connect').onclick = async () => {{
  try {{
    if (!window.ethereum) throw new Error('MetaMask not detected.');
    const accts = await ethereum.request({{ method: 'eth_requestAccounts' }});
    account = accts[0];
    log('Connected: ' + account);
    await ensureChain();
    document.getElementById('pay').disabled = false;
  }} catch (e) {{
    log('Connect error: ' + (e?.message || e));
    alert('Connect error: ' + (e?.message || e));
  }}
}};

document.getElementById('pay').onclick = async () => {{
  try {{
    if (!window.ethereum) throw new Error('MetaMask not detected.');
    await ensureChain();

    // --- 1) Log params ---
    console.log({{ TO, VALUE_HEX, CHAIN_ID_HEX, account }});

    // --- 2) Preflight: balance, gas, gasPrice ---
    const balHex = await ethereum.request({{ method: 'eth_getBalance', params: [account, 'latest'] }});
    const gasPriceHex = await ethereum.request({{ method: 'eth_gasPrice' }});
    const value = BigInt(VALUE_HEX);
    const balance = BigInt(balHex);
    const gasPrice = BigInt(gasPriceHex);

    // Estimate gas for a simple native transfer; fall back to 21000 if estimate fails
    let gasHex;
    try {{
      gasHex = await ethereum.request({{
        method: 'eth_estimateGas',
        params: [{{ from: account, to: TO, value: VALUE_HEX, data: '0x' }}]
      }});
    }} catch {{
      gasHex = '0x5208'; // 21000
    }}
    const gas = BigInt(gasHex);

    const needed = value + gas * gasPrice;
    if (balance < needed) {{
      const fmt = (wei) => Number(wei) / 1e18;
      alert(
        `Insufficient CHZ on Spicy:\n` +
        `Balance: ${{fmt(balance)}}\n` +
        `Needed (value + gas): ${{fmt(needed)}}`
      );
      return;
    }}

    // --- 3) Send transaction (explicit gas helps some RPCs) ---
    const params = {{ from: account, to: TO, value: VALUE_HEX, data: '0x', gas: gasHex }};
    const hash = await ethereum.request({{ method: 'eth_sendTransaction', params: [params] }});
    log('Sent tx: ' + hash);

    await sleep(10000);

    // --- 4) Report tx hash ---
    const rep = await fetch(`${{API_BASE}}/api/crypto/intents/${{PID}}/report-tx`, {{
      method: 'POST',
      headers: {{ 'Content-Type': 'application/json' }},
      body: JSON.stringify({{ txHash: hash }})
    }});
    if (!rep.ok) {{
      const t = await rep.text();
      throw new Error('Report failed: ' + t);
    }}
    log('Reported tx hash.');

    // --- 5) Capture (reconcile) ---
    const rec = await fetch(`${{API_BASE}}/api/space/${{SPACE_ID}}/orders/${{ORDER_ID}}/reconcile`, {{
      method: 'POST',
      headers: {{ 'Content-Type': 'application/json' }},
      body: JSON.stringify({{
        providerIntentId: PID,
        intentId: INTENT_ID,
        idempotencyKey: 'idem-cap-' + Date.now(),
        attemptCaptureIfApproved: true
      }})
    }});
    if (!rec.ok) {{
      const t = await rec.text();
      log('Reconcile failed: ' + t);
      alert('Reconcile failed (maybe waiting for confirmations). Try again shortly.');
      return;
    }}

    log('Payment captured!');
    alert('Payment captured!');
  }} catch (e) {{
    console.error(e);
    // Show the underlying RPC message if present
    const msg = e?.data?.message || e?.message || String(e);
    log('Pay error: ' + msg);
    alert('Pay error: ' + msg);
  }}
}};

</script>
</body></html>";

            return Content(html, "text/html; charset=utf-8");
        }
    }
}
