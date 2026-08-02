using LLMPay.Domain.Entities;

namespace LLMPay.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User> AddAsync(User user);
    Task<User> UpdateAsync(User user);
    Task DeleteAsync(User user);
    Task<bool> EmailExistsAsync(string email);
}

public interface IPaymentOrderRepository
{
    Task<PaymentOrder?> GetByIdAsync(Guid id);
    Task<PaymentOrder?> GetByOrderNumberAsync(string orderNumber);
    Task<PaymentOrder?> GetByStripeSessionIdAsync(string sessionId);
    Task<IEnumerable<PaymentOrder>> GetUserOrdersAsync(Guid userId);
    Task<PaymentOrder> AddAsync(PaymentOrder order);
    Task<PaymentOrder> UpdateAsync(PaymentOrder order);
    Task<int> GetUserCompletedOrdersCountAsync(Guid userId);
    Task<decimal> GetUserTotalSpentAsync(Guid userId);
}

public interface IWalletRepository
{
    Task<IReadOnlyList<CreditTransaction>> GetTransactionsAsync(Guid userId, int take = 100);
    Task<bool> CreditRechargeAsync(Guid orderId, string stripePaymentIntentId);
}
