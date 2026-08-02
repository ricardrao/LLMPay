using LLMPay.Core.Interfaces;
using LLMPay.Core.Services;
using LLMPay.Domain.Entities;

namespace LLMPay.Tests;

public class PaymentOrderServiceTests
{
    [Fact]
    public async Task CreateRechargeOrder_UsesAuthenticatedUserAndCreditsOneToOne()
    {
        var repository = new FakeOrders();
        var service = new PaymentOrderService(repository);
        var userId = Guid.NewGuid();

        var order = await service.CreateRechargeOrderAsync(userId, 25.12m, "USD", "127.0.0.1", "test");

        Assert.Equal(userId, order.UserId);
        Assert.Equal(25.12m, order.Amount);
        Assert.Equal(25.12m, order.Credits);
        Assert.Equal("usd", order.Currency);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10001)]
    public async Task CreateRechargeOrder_RejectsUnsafeAmounts(decimal amount)
    {
        var service = new PaymentOrderService(new FakeOrders());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.CreateRechargeOrderAsync(Guid.NewGuid(), amount, "usd", "ip", "ua"));
    }

    private sealed class FakeOrders : IPaymentOrderRepository
    {
        private readonly List<PaymentOrder> _orders = [];
        public Task<PaymentOrder> AddAsync(PaymentOrder order) { _orders.Add(order); return Task.FromResult(order); }
        public Task<PaymentOrder?> GetByIdAsync(Guid id) => Task.FromResult(_orders.FirstOrDefault(x => x.Id == id));
        public Task<PaymentOrder?> GetByOrderNumberAsync(string number) => Task.FromResult(_orders.FirstOrDefault(x => x.OrderNumber == number));
        public Task<PaymentOrder?> GetByStripeSessionIdAsync(string id) => Task.FromResult(_orders.FirstOrDefault(x => x.StripeSessionId == id));
        public Task<IEnumerable<PaymentOrder>> GetUserOrdersAsync(Guid id) => Task.FromResult<IEnumerable<PaymentOrder>>(_orders.Where(x => x.UserId == id));
        public Task<PaymentOrder> UpdateAsync(PaymentOrder order) => Task.FromResult(order);
        public Task<int> GetUserCompletedOrdersCountAsync(Guid id) => Task.FromResult(0);
        public Task<decimal> GetUserTotalSpentAsync(Guid id) => Task.FromResult(0m);
    }
}
