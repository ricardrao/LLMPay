namespace LLMPay.Domain.Entities;

/// <summary>一次性 LLMPay 平台余额兑换卡。数据库仅保存卡密哈希。</summary>
public class RechargeCard
{
    public Guid Id { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public string CodeSuffix { get; set; } = string.Empty;
    public decimal Credits { get; set; }
    public string Currency { get; set; } = "usd";
    public RechargeCardStatus Status { get; set; } = RechargeCardStatus.Active;
    public string? BatchName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public Guid? RedeemedByUserId { get; set; }
    public DateTime? RedeemedAt { get; set; }
    public virtual User? RedeemedByUser { get; set; }
}

public enum RechargeCardStatus
{
    Active = 1,
    Redeemed = 2,
    Disabled = 3,
    Expired = 4
}
