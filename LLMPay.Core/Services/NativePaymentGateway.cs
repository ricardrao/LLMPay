namespace LLMPay.Core.Services;

public record NativePaymentRequest(string Provider, string OrderNumber, string Description, decimal AmountCny, string NotifyUrl);
public record NativePaymentResult(bool Success, string? CodeUrl, string? GatewayTradeNo, string Mode, string? Error);
public record PaymentNotificationResult(bool Valid, bool Paid, string? OrderNumber, string? GatewayTradeNo, decimal? AmountCny, string? Error);

public interface INativePaymentGateway
{
    string Provider { get; }
    bool IsConfigured { get; }
    Task<NativePaymentResult> CreateAsync(NativePaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentNotificationResult> VerifyNotificationAsync(string body, IReadOnlyDictionary<string,string> headers, IReadOnlyDictionary<string,string> form, CancellationToken cancellationToken = default);
}
