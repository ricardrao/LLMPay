using Microsoft.EntityFrameworkCore;
using LLMPay.Domain.Entities;
using LLMPay.Core.Interfaces;

namespace LLMPay.Infrastructure.Repositories;

public class PaymentOrderRepository : RepositoryBase<PaymentOrder>, IPaymentOrderRepository
{
    public PaymentOrderRepository(Data.LlmPayDbContext context) : base(context)
    {
    }

    public async Task<PaymentOrder?> GetByOrderNumberAsync(string orderNumber)
    {
        return await DbSet
            .Include(po => po.User)
            .FirstOrDefaultAsync(po => po.OrderNumber == orderNumber);
    }

    public async Task<PaymentOrder?> GetByStripeSessionIdAsync(string sessionId)
    {
        return await DbSet
            .Include(po => po.User)
            .FirstOrDefaultAsync(po => po.StripeSessionId == sessionId);
    }

    public async Task<IEnumerable<PaymentOrder>> GetUserOrdersAsync(Guid userId)
    {
        return await DbSet
            .Where(po => po.UserId == userId)
            .OrderByDescending(po => po.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetUserCompletedOrdersCountAsync(Guid userId)
    {
        return await DbSet
            .Where(po => po.UserId == userId && po.Status == OrderStatus.Completed)
            .CountAsync();
    }

    public async Task<decimal> GetUserTotalSpentAsync(Guid userId)
    {
        return await DbSet
            .Where(po => po.UserId == userId && po.Status == OrderStatus.Completed)
            .SumAsync(po => po.Amount);
    }
}
