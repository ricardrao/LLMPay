using LLMPay.Core.Interfaces;
using LLMPay.Domain.Entities;
using LLMPay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LLMPay.Infrastructure.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly LlmPayDbContext _context;

    public WalletRepository(LlmPayDbContext context) => _context = context;

    public async Task<IReadOnlyList<CreditTransaction>> GetTransactionsAsync(Guid userId, int take = 100) =>
        await _context.CreditTransactions.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync();

    public async Task<bool> CreditRechargeAsync(Guid orderId, string stripePaymentIntentId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var order = await _context.PaymentOrders.FirstOrDefaultAsync(x => x.Id == orderId);
        if (order is null) return false;
        if (order.Status == OrderStatus.Completed) return true;

        var user = await _context.Users.FirstAsync(x => x.Id == order.UserId);
        user.Balance += order.Credits;
        order.Status = OrderStatus.Completed;
        order.CompletedAt = DateTime.UtcNow;
        order.StripePaymentIntentId = stripePaymentIntentId;
        _context.CreditTransactions.Add(new CreditTransaction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Amount = order.Credits,
            BalanceAfter = user.Balance,
            Type = CreditTransactionType.Recharge,
            ReferenceId = $"recharge:{order.Id}",
            Description = $"Stripe recharge {order.OrderNumber}"
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }
}
