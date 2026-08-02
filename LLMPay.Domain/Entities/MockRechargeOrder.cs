namespace LLMPay.Domain.Entities;

/// <summary>开发环境专用的模拟订阅充值订单。绝不存储会话 Token。</summary>
public class MockRechargeOrder
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string? GuestAccessHash { get; set; }
    public bool AccountProvisioned { get; set; }
    public string TargetEmail { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string MockAccountId { get; set; } = string.Empty;
    public string Plan { get; set; } = "Plus";
    public string PaymentProvider { get; set; } = "wechat";
    public decimal Amount { get; set; }
    public MockRechargeStatus Status { get; set; } = MockRechargeStatus.Created;
    public string? MockCheckoutSessionId { get; set; }
    public string? MockSubscriptionId { get; set; }
    public string? RechargeCodeHash { get; set; }
    public string? RechargeCodeSuffix { get; set; }
    public string? RechargeCodeProtected { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public virtual User? User { get; set; }
}

public enum MockRechargeStatus
{
    Created = 1,
    AccountVerified = 2,
    CheckoutCreated = 3,
    PaymentConfirmed = 4,
    SubscriptionActivated = 5,
    Failed = 6,
    AccountConfirmed = 7,
    Cancelled = 8,
    CardVerified = 9
}
