namespace LLMPay.Domain.Entities;

/// <summary>
/// 平台余额充值订单
/// </summary>
public class PaymentOrder
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// 订单号（唯一标识，用于外部查询）
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// 关联的用户ID
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// 支付金额（主货币单位）
    /// </summary>
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "usd";

    /// <summary>支付成功后加入的平台余额。</summary>
    public decimal Credits { get; set; }
    
    /// <summary>
    /// 支付方式：Stripe, CreditCard等
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Stripe;
    
    /// <summary>
    /// 订单状态：Pending, Processing, Completed, Failed, Cancelled
    /// </summary>
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    /// <summary>
    /// Stripe Checkout Session ID
    /// </summary>
    public string? StripeSessionId { get; set; }
    
    /// <summary>
    /// Stripe Payment Intent ID
    /// </summary>
    public string? StripePaymentIntentId { get; set; }
    
    /// <summary>
    /// 订单创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 订单完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// 错误信息（如果支付失败）
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// IP地址（用于风控）
    /// </summary>
    public string? ClientIp { get; set; }
    
    /// <summary>
    /// User Agent（浏览器信息）
    /// </summary>
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// 导航属性：关联的用户
    /// </summary>
    public virtual User User { get; set; } = null!;
    
    /// <summary>
}

public enum PaymentMethod
{
    Stripe = 1,
    CreditCard = 2,
    PayPal = 3
}

public enum OrderStatus
{
    Pending = 1,        // 待支付
    Processing = 2,     // 处理中
    Completed = 3,      // 已完成
    Failed = 4,         // 支付失败
    Cancelled = 5,      // 已取消
    Refunding = 6,      // 退款中
    Refunded = 7        // 已退款
}
