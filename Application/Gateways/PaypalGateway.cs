using PaypalServerSdk.Standard;
using PaypalServerSdk.Standard.Models;
using TifoXRCoreWebAPI.Utilities.PaymentGateways;
using PpAppContext = PaypalServerSdk.Standard.Models.OrderApplicationContext;

namespace TifoXRCoreWebAPI.Application.PaymentGateways
{
    public sealed class PaypalGateway : IPaymentGateway
    {
        private readonly PaypalServerSdkClient _sdk;
        public string Name => "paypal";

        public PaypalGateway(IPayPalClientFactory factory) => _sdk = factory.GetClient();

        public async Task<CreateGatewayIntentResult> CreateIntentAsync(CreateGatewayIntentRequest req)
        {
            var orderReq = new OrderRequest
            {
                Intent = CheckoutPaymentIntent.Capture,
                PurchaseUnits = new()
                {
                    new PurchaseUnitRequest
                    {
                        Amount = new AmountWithBreakdown
                        {
                            CurrencyCode = "USD", //req.CurrencyId, //FIX ME: Add a resolver to generate ISO currency code from CurrencyId
                            MValue = req.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                        }
                    }
                },
                ApplicationContext = new PpAppContext
                {
                    ReturnUrl = req.ReturnUrl,
                    CancelUrl = req.CancelUrl
                }
            };

            var createInput = new CreateOrderInput(
                contentType: "application/json",
                body: orderReq,
                paypalRequestId: req.IdempotencyKey,
                prefer: "return=representation");

            var created = await _sdk.OrdersController.CreateOrderAsync(createInput);
            var providerOrderId = created.Data.Id;
            var approve = created.Data.Links?.FirstOrDefault(l => l.Rel == "approve")?.Href;

            return new CreateGatewayIntentResult(providerOrderId, approve);
        }

        public async Task<CaptureGatewayResult> CaptureAsync(CaptureGatewayRequest req)
        {
            var captureInput = new CaptureOrderInput(
                id: req.ProviderIntentId,
                contentType: "application/json",
                paypalRequestId: req.IdempotencyKey,
                prefer: "return=representation");

            var captured = await _sdk.OrdersController.CaptureOrderAsync(captureInput);

            var cap = captured?.Data?.PurchaseUnits?
                .SelectMany(u => u.Payments?.Captures ?? Enumerable.Empty<OrdersCapture>())
                .FirstOrDefault();

            if (cap is null) 
                throw new InvalidOperationException("Capture not returned by PayPal.");
            
            var amount = decimal.Parse(cap.Amount?.MValue ?? "0.00", System.Globalization.CultureInfo.InvariantCulture);
            var iso = cap.Amount?.CurrencyCode ?? "USD";

            return new CaptureGatewayResult(cap.Id, amount, iso);   //FIX ME: Add a resolver to generate CurrencyId from ISO code
        }

        public async Task<GatewayIntentStatusResult?> GetIntentAsync(string providerIntentId)
        {
            var got = await _sdk.OrdersController.GetOrderAsync(new GetOrderInput(providerIntentId));
            var status = got?.Data?.Status;
            var approve = got?.Data?.Links?.FirstOrDefault(l => l.Rel == "approve")?.Href;

            if (status == OrderStatus._Unknown) return null;

            // TO DO: Add a demo data and send that as response

            return new GatewayIntentStatusResult(providerIntentId, status!.Value.ToString(), approve);
        }

        public Task<RefundGatewayResult> RefundAsync(RefundGatewayRequest req)
        {
            // For PayPal "Orders" API, refunds are done on captures via "RefundsController" (if available in your SDK).
            // Implement when you wire refunds; returning NotImplemented here keeps compile happy until then.
            throw new NotImplementedException("PayPal refund via capture not implemented in this sample.");
        }
    }
}
