using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LLMPay.Api.Services;
using Microsoft.Extensions.Configuration;

namespace LLMPay.Tests;

public class StripeCheckoutServiceTests
{
    [Fact]
    public void VerifyWebhook_AcceptsValidPaidEvent()
    {
        const string secret = "whsec_test_secret";
        var orderId = Guid.NewGuid();
        var body = JsonSerializer.Serialize(new
        {
            type = "checkout.session.completed",
            data = new { @object = new { payment_status = "paid", payment_intent = "pi_123", metadata = new { order_id = orderId } } }
        });
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var digest = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{body}"))).ToLowerInvariant();
        var service = CreateService(secret);

        var result = service.VerifyAndParseWebhook(body, $"t={timestamp},v1={digest}");

        Assert.True(result.Valid);
        Assert.True(result.Paid);
        Assert.Equal(orderId, result.OrderId);
        Assert.Equal("pi_123", result.PaymentIntentId);
    }

    [Fact]
    public void VerifyWebhook_RejectsBadSignature()
    {
        var result = CreateService("whsec_test_secret").VerifyAndParseWebhook("{}", $"t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()},v1=bad");
        Assert.False(result.Valid);
    }

    private static StripeCheckoutService CreateService(string secret)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Stripe:WebhookSecret"] = secret
        }).Build();
        return new StripeCheckoutService(new HttpClient(), configuration);
    }
}
