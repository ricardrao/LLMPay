namespace LLMPay.Domain.Entities;

/// <summary>
/// OpenAI API 服务平台用户实体
/// </summary>
public class User
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// 用户邮箱（唯一标识）
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// 密码哈希
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户昵称
    /// </summary>
    public string? Nickname { get; set; }
    
    /// <summary>
    /// 平台余额（单位由 Currency 约定；不是 OpenAI 官方账户余额）
    /// </summary>
    public decimal Balance { get; set; }
    
    /// <summary>
    /// 账户状态：Active, Suspended, Deleted
    /// </summary>
    public UserStatus Status { get; set; } = UserStatus.Active;

    /// <summary>平台权限角色。</summary>
    public UserRole Role { get; set; } = UserRole.User;
    
    /// <summary>
    /// 是否验证邮箱
    /// </summary>
    public bool EmailVerified { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// 绑定的Stripe账户ID
    /// </summary>
    public string? StripeCustomerId { get; set; }
    public string? PasswordSetupTokenHash { get; set; }
    public DateTime? PasswordSetupExpiresAt { get; set; }
    
    /// <summary>
    /// 导航属性：用户的订单
    /// </summary>
    public ICollection<PaymentOrder> PaymentOrders { get; set; } = new List<PaymentOrder>();

    public ICollection<CreditTransaction> CreditTransactions { get; set; } = new List<CreditTransaction>();
    public ICollection<RechargeCard> RedeemedCards { get; set; } = new List<RechargeCard>();
    public ICollection<CardPurchaseOrder> CardPurchaseOrders { get; set; } = new List<CardPurchaseOrder>();
    public ICollection<MockRechargeOrder> MockRechargeOrders { get; set; } = new List<MockRechargeOrder>();
}

public enum UserStatus
{
    Active = 1,
    Suspended = 2,
    Deleted = 3
}

public enum UserRole
{
    User = 1,
    Admin = 2
}
