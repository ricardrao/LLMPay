namespace LLMPay.Core.Services;

public record CheckoutResult(bool Success, string? Url, string? SessionId, string? Error);
public record StripeEventResult(bool Valid, bool Paid, string? EventType, Guid? OrderId, string? PaymentIntentId, string? Error);

public interface IRechargePaymentService
{
    Task<CheckoutResult> CreateCheckoutAsync(Guid orderId, string orderNumber, decimal amount, string currency, string customerEmail);
    StripeEventResult VerifyAndParseWebhook(string body, string signature);
}
