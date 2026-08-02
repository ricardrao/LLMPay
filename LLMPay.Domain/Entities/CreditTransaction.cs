namespace LLMPay.Domain.Entities;

/// <summary>平台余额账本。记录一切入账、消费与退款，不保存 OpenAI/ChatGPT 登录凭据。</summary>
public class CreditTransaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public CreditTransactionType Type { get; set; }
    public string ReferenceId { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public virtual User User { get; set; } = null!;
}

public enum CreditTransactionType
{
    Recharge = 1,
    ApiUsage = 2,
    Refund = 3,
    Adjustment = 4
}
