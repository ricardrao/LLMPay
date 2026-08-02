using System.Security.Cryptography;
using System.Text;
using LLMPay.Domain.Entities;
using LLMPay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LLMPay.Infrastructure.Services;

public record GeneratedCard(Guid Id, string Code, decimal Credits, DateTime ExpiresAt);
public record RedeemCardResult(bool Success, string Message, decimal Credits, decimal BalanceAfter, string CodeSuffix);

public class RechargeCardService
{
    private readonly LlmPayDbContext _db;
    public RechargeCardService(LlmPayDbContext db) => _db = db;

    public async Task<IReadOnlyList<GeneratedCard>> GenerateAsync(decimal credits, int count, string? batchName, int validDays = 365)
    {
        if (credits <= 0 || credits > 10_000) throw new ArgumentOutOfRangeException(nameof(credits));
        if (count < 1 || count > 100) throw new ArgumentOutOfRangeException(nameof(count));
        var output = new List<GeneratedCard>();
        for (var i = 0; i < count; i++)
        {
            var code = GenerateCode();
            var card = new RechargeCard
            {
                Id = Guid.NewGuid(), CodeHash = Hash(code), CodeSuffix = code[^4..], Credits = credits,
                Currency = "usd", BatchName = batchName, ExpiresAt = DateTime.UtcNow.AddDays(validDays)
            };
            _db.RechargeCards.Add(card);
            output.Add(new(card.Id, code, credits, card.ExpiresAt));
        }
        await _db.SaveChangesAsync();
        return output;
    }

    public async Task<RedeemCardResult> RedeemAsync(Guid userId, string rawCode)
    {
        var normalized = Normalize(rawCode);
        if (normalized.Length != 16) return new(false, "卡密格式不正确", 0, 0, string.Empty);
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var card = await _db.RechargeCards.FirstOrDefaultAsync(x => x.CodeHash == Hash(normalized));
        if (card is null) return new(false, "卡密不存在或输入错误", 0, 0, string.Empty);
        if (card.Status == RechargeCardStatus.Redeemed) return new(false, "该卡密已被使用", 0, 0, card.CodeSuffix);
        if (card.Status == RechargeCardStatus.Disabled) return new(false, "该卡密已停用", 0, 0, card.CodeSuffix);
        if (card.ExpiresAt <= DateTime.UtcNow)
        {
            card.Status = RechargeCardStatus.Expired; await _db.SaveChangesAsync();
            return new(false, "该卡密已过期", 0, 0, card.CodeSuffix);
        }
        var user = await _db.Users.FirstAsync(x => x.Id == userId);
        user.Balance += card.Credits;
        card.Status = RechargeCardStatus.Redeemed; card.RedeemedByUserId = userId; card.RedeemedAt = DateTime.UtcNow;
        _db.CreditTransactions.Add(new CreditTransaction
        {
            Id = Guid.NewGuid(), UserId = userId, Amount = card.Credits, BalanceAfter = user.Balance,
            Type = CreditTransactionType.Recharge, ReferenceId = $"card:{card.Id}", Description = $"余额卡兑换 •••• {card.CodeSuffix}"
        });
        await _db.SaveChangesAsync(); await transaction.CommitAsync();
        return new(true, "兑换成功", card.Credits, user.Balance, card.CodeSuffix);
    }

    public static string Normalize(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(value)))).ToLowerInvariant();
    public static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<byte> bytes = stackalloc byte[16]; RandomNumberGenerator.Fill(bytes);
        var raw = new string(bytes.ToArray().Select(x => chars[x % chars.Length]).ToArray());
        return $"{raw[..4]}-{raw[4..8]}-{raw[8..12]}-{raw[12..]}";
    }
}
