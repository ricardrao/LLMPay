using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LLMPay.Core.Services;

namespace LLMPay.Api.Services;

public class StripeCheckoutService : IRechargePaymentService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;

    public StripeCheckoutService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
    }

    public async Task<CheckoutResult> CreateCheckoutAsync(Guid orderId, string orderNumber, decimal amount, string currency, string customerEmail)
    {
        var secret = _configuration["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Contains("your_", StringComparison.OrdinalIgnoreCase))
            return new(false, null, null, "Stripe is not configured.");

        var baseUrl = _configuration["PublicBaseUrl"]?.TrimEnd('/') ?? "https://localhost:5001";
        var cents = checked((long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));
        var form = new Dictionary<string, string>
        {
            ["mode"] = "payment",
            ["success_url"] = $"{baseUrl}/payment/success?session_id={{CHECKOUT_SESSION_ID}}",
            ["cancel_url"] = $"{baseUrl}/payment/cancel",
            ["customer_email"] = customerEmail,
            ["client_reference_id"] = orderId.ToString(),
            ["metadata[order_id]"] = orderId.ToString(),
            ["line_items[0][quantity]"] = "1",
            ["line_items[0][price_data][currency]"] = currency,
            ["line_items[0][price_data][unit_amount]"] = cents.ToString(CultureInfo.InvariantCulture),
            ["line_items[0][price_data][product_data][name]"] = $"LLMPay platform credits - {orderNumber}"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/checkout/sessions") { Content = new FormUrlEncodedContent(form) };
        request.Headers.Authorization = new("Bearer", secret);
        using var response = await _http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) return new(false, null, null, $"Stripe error ({(int)response.StatusCode}).");

        using var json = JsonDocument.Parse(body);
        return new(true, json.RootElement.GetProperty("url").GetString(), json.RootElement.GetProperty("id").GetString(), null);
    }

    public StripeEventResult VerifyAndParseWebhook(string body, string signature)
    {
        var secret = _configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(signature)) return new(false, false, null, null, null, "Missing webhook configuration or signature.");

        var parts = signature.Split(',').Select(x => x.Split('=', 2)).Where(x => x.Length == 2).ToLookup(x => x[0], x => x[1]);
        var timestampText = parts["t"].FirstOrDefault();
        if (!long.TryParse(timestampText, out var timestamp) || Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestamp) > 300)
            return new(false, false, null, null, null, "Webhook timestamp is invalid.");

        var signed = Encoding.UTF8.GetBytes($"{timestampText}.{body}");
        var expected = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), signed)).ToLowerInvariant();
        var valid = parts["v1"].Any(value => value.Length == expected.Length && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(value), Encoding.ASCII.GetBytes(expected)));
        if (!valid) return new(false, false, null, null, null, "Webhook signature is invalid.");

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        var type = root.GetProperty("type").GetString();
        var session = root.GetProperty("data").GetProperty("object");
        var paid = session.TryGetProperty("payment_status", out var paymentStatus) && paymentStatus.GetString() == "paid";
        Guid? orderId = null;
        if (session.TryGetProperty("metadata", out var metadata) && metadata.TryGetProperty("order_id", out var rawOrderId) && Guid.TryParse(rawOrderId.GetString(), out var parsed)) orderId = parsed;
        var paymentIntent = session.TryGetProperty("payment_intent", out var pi) && pi.ValueKind == JsonValueKind.String ? pi.GetString() : null;
        return new(true, paid, type, orderId, paymentIntent, null);
    }
}
