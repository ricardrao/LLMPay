using LLMPay.Core.Interfaces;
using LLMPay.Domain.Entities;

namespace LLMPay.Core.Services;

public interface IPaymentOrderService
{
    Task<PaymentOrder> CreateRechargeOrderAsync(Guid userId, decimal amount, string currency, string clientIp, string userAgent);
    Task<PaymentOrder?> GetOrderByNumberAsync(string orderNumber);
    Task<IEnumerable<PaymentOrder>> GetUserOrdersAsync(Guid userId);
    Task SetStripeSessionAsync(Guid orderId, string sessionId);
    Task MarkFailedAsync(Guid orderId, string error);
}

public class PaymentOrderService : IPaymentOrderService
{
    private readonly IPaymentOrderRepository _orders;

    public PaymentOrderService(IPaymentOrderRepository orders) => _orders = orders;

    public async Task<PaymentOrder> CreateRechargeOrderAsync(Guid userId, decimal amount, string currency, string clientIp, string userAgent)
    {
        if (amount < 1 || amount > 10_000) throw new ArgumentOutOfRangeException(nameof(amount), "Recharge amount must be between 1 and 10000.");
        currency = currency.Trim().ToLowerInvariant();
        if (currency is not ("usd" or "cny")) throw new ArgumentException("Only usd and cny are supported.", nameof(currency));

        return await _orders.AddAsync(new PaymentOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"LLP{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{Random.Shared.Next(1000, 9999)}",
            UserId = userId,
            Amount = decimal.Round(amount, 2),
            Credits = decimal.Round(amount, 2),
            Currency = currency,
            ClientIp = clientIp,
            UserAgent = userAgent,
            Status = OrderStatus.Pending
        });
    }

    public Task<PaymentOrder?> GetOrderByNumberAsync(string orderNumber) => _orders.GetByOrderNumberAsync(orderNumber);
    public Task<IEnumerable<PaymentOrder>> GetUserOrdersAsync(Guid userId) => _orders.GetUserOrdersAsync(userId);

    public async Task SetStripeSessionAsync(Guid orderId, string sessionId)
    {
        var order = await _orders.GetByIdAsync(orderId) ?? throw new InvalidOperationException("Order not found.");
        order.StripeSessionId = sessionId;
        order.Status = OrderStatus.Processing;
        await _orders.UpdateAsync(order);
    }

    public async Task MarkFailedAsync(Guid orderId, string error)
    {
        var order = await _orders.GetByIdAsync(orderId);
        if (order is null || order.Status == OrderStatus.Completed) return;
        order.Status = OrderStatus.Failed;
        order.ErrorMessage = error;
        await _orders.UpdateAsync(order);
    }
}
