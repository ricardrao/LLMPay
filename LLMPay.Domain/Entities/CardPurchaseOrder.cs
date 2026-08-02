namespace LLMPay.Domain.Entities;

/// <summary>余额卡购买订单。支付回调成功后才发放一次性卡密。</summary>
public class CardPurchaseOrder
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public decimal Credits { get; set; }
    public string Currency { get; set; } = "cny";
    public PaymentProvider Provider { get; set; }
    public CardPurchaseStatus Status { get; set; } = CardPurchaseStatus.AwaitingPayment;
    public string? ProviderTradeNumber { get; set; }
    public string? IssuedCardCodeProtected { get; set; }
    public Guid? IssuedCardId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public virtual User User { get; set; } = null!;
    public virtual RechargeCard? IssuedCard { get; set; }
}

public enum PaymentProvider { WeChatPay = 1, Alipay = 2 }
public enum CardPurchaseStatus { AwaitingPayment = 1, Paid = 2, CardIssued = 3, Cancelled = 4, Failed = 5 }
