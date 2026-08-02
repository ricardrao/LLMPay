using LLMPay.Domain.Entities;
using LLMPay.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace LLMPay.Infrastructure.Services;

public record CardPurchaseView(Guid Id, string OrderNumber, decimal Amount, decimal Credits, string Currency, string Provider, string Status, string? PaymentUrl, string? CardCode, DateTime CreatedAt);

public class CardPurchaseService
{
    private readonly LlmPayDbContext _db;
    private readonly IDataProtector _protector;
    public CardPurchaseService(LlmPayDbContext db, IDataProtectionProvider protection) { _db = db; _protector = protection.CreateProtector("LLMPay.CardPurchase.Code.v1"); }

    public async Task<CardPurchaseView> CreateAsync(Guid userId, decimal credits, PaymentProvider provider, bool development)
    {
        if (credits is not (10 or 25 or 50 or 100)) throw new ArgumentException("请选择有效套餐");
        var order = new CardPurchaseOrder
        {
            Id = Guid.NewGuid(), OrderNumber = $"CARD{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{Random.Shared.Next(1000,9999)}",
            UserId = userId, Amount = credits, Credits = credits, Provider = provider, Currency = "cny"
        };
        _db.CardPurchaseOrders.Add(order); await _db.SaveChangesAsync();
        var paymentUrl = development ? $"/api/card-purchases/{order.Id}/simulate-payment" : null;
        return ToView(order, paymentUrl);
    }

    public async Task<CardPurchaseView?> GetAsync(Guid userId, Guid orderId)
    {
        var order = await _db.CardPurchaseOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == orderId && x.UserId == userId);
        return order is null ? null : ToView(order, null);
    }

    public async Task<CardPurchaseView> ConfirmPaidAsync(Guid orderId, string providerTradeNumber)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var order = await _db.CardPurchaseOrders.FirstOrDefaultAsync(x => x.Id == orderId) ?? throw new InvalidOperationException("订单不存在");
        if (order.Status == CardPurchaseStatus.CardIssued) return ToView(order, null);
        if (order.Status != CardPurchaseStatus.AwaitingPayment) throw new InvalidOperationException("订单状态不允许支付");

        var code = RechargeCardService.GenerateCode();
        var card = new RechargeCard
        {
            Id = Guid.NewGuid(), CodeHash = HashCode(code), CodeSuffix = code[^4..], Credits = order.Credits,
            Currency = "usd", BatchName = $"购买订单 {order.OrderNumber}", ExpiresAt = DateTime.UtcNow.AddDays(365)
        };
        _db.RechargeCards.Add(card);
        order.Status = CardPurchaseStatus.CardIssued; order.PaidAt = DateTime.UtcNow;
        order.ProviderTradeNumber = providerTradeNumber; order.IssuedCardId = card.Id;
        order.IssuedCardCodeProtected = _protector.Protect(code);
        await _db.SaveChangesAsync(); await transaction.CommitAsync();
        return ToView(order, null);
    }

    private CardPurchaseView ToView(CardPurchaseOrder order, string? paymentUrl)
    {
        string? code = null;
        if (order.Status == CardPurchaseStatus.CardIssued && !string.IsNullOrEmpty(order.IssuedCardCodeProtected))
            code = _protector.Unprotect(order.IssuedCardCodeProtected);
        return new(order.Id, order.OrderNumber, order.Amount, order.Credits, order.Currency, order.Provider.ToString(), order.Status.ToString(), paymentUrl, code, order.CreatedAt);
    }

    private static string HashCode(string value) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(RechargeCardService.Normalize(value)))).ToLowerInvariant();
}
