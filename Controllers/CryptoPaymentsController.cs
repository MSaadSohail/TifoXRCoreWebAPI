using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.Extensions.Options;
using GMS.TifoXRCoreWebAPI.Application.PaymentGateways;

[ApiController]
[Route("api/crypto")]
public sealed class CryptoPaymentsController : ControllerBase
{
    private readonly CryptoChilizGateway _gateway;

    public CryptoPaymentsController(IEnumerable<IPaymentGateway> gateways)
    {
        // resolve our concrete gateway from the registered set
        _gateway = gateways.OfType<CryptoChilizGateway>().FirstOrDefault()
                   ?? throw new InvalidOperationException("CryptoChilizGateway not registered.");
    }

    /// <summary>Fetch the prepared thirdweb Bridge payload for client-side execution.</summary>
    [HttpGet("intents/{pid}")]
    public async Task<IActionResult> GetPrepared(string pid, [FromQuery] string sender)
    {
        if (string.IsNullOrWhiteSpace(sender))
            return BadRequest(new { message = "Query ?sender=0xYourWallet is required." });

        var json = await _gateway.GetPreparedJsonAsync(pid, sender);
        return Content(json, "application/json");
    }


    /// <summary>Client reports the on-chain tx hash after executing the prepared steps.</summary>
    public sealed record ReportTxRequest(string TxHash);

    [HttpPost("intents/{pid}/report-tx")]
    public async Task<IActionResult> ReportTx(string pid, [FromBody] ReportTxRequest req)
    {
        await _gateway.ReportTxAsync(pid, req.TxHash);
        return NoContent();
    }
}
