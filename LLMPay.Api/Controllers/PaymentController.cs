using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LLMPay.Core.Interfaces;
using LLMPay.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LLMPay.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentOrderService _orders;
    private readonly IRechargePaymentService _payments;
    private readonly IWalletRepository _wallet;
    private readonly IUserService _users;

    public PaymentController(IPaymentOrderService orders, IRechargePaymentService payments, IWalletRepository wallet, IUserService users)
    {
        _orders = orders;
        _payments = payments;
        _wallet = wallet;
        _users = users;
    }

    [Authorize]
    [HttpPost("recharge-checkout")]
    public async Task<IActionResult> CreateRechargeCheckout(CreateRechargeRequest request)
    {
        var userId = CurrentUserId();
        var user = await _users.GetUserAsync(userId);
        if (user is null) return Unauthorized();

        try
        {
            var order = await _orders.CreateRechargeOrderAsync(userId, request.Amount, request.Currency,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown", Request.Headers.UserAgent.ToString());
            var checkout = await _payments.CreateCheckoutAsync(order.Id, order.OrderNumber, order.Amount, order.Currency, user.Email);
            if (!checkout.Success)
            {
                await _orders.MarkFailedAsync(order.Id, checkout.Error ?? "Checkout failed.");
                return BadRequest(new { checkout.Error });
            }
            await _orders.SetStripeSessionAsync(order.Id, checkout.SessionId!);
            return Ok(new { order.OrderNumber, checkout.Url, checkout.SessionId });
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [Authorize]
    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders()
    {
        var orders = await _orders.GetUserOrdersAsync(CurrentUserId());
        return Ok(orders.Select(x => new { x.OrderNumber, x.Amount, x.Currency, x.Credits, status = x.Status.ToString(), x.CreatedAt, x.CompletedAt }));
    }

    [Authorize]
    [HttpGet("wallet")]
    public async Task<IActionResult> GetWallet()
    {
        var userId = CurrentUserId();
        var user = await _users.GetUserAsync(userId);
        return user is null ? Unauthorized() : Ok(new { user.Balance, transactions = await _wallet.GetTransactionsAsync(userId) });
    }

    [AllowAnonymous]
    [HttpPost("webhook/stripe")]
    public async Task<IActionResult> StripeWebhook()
    {
        var body = await new StreamReader(Request.Body).ReadToEndAsync();
        var parsed = _payments.VerifyAndParseWebhook(body, Request.Headers["Stripe-Signature"].ToString());
        if (!parsed.Valid) return BadRequest(new { parsed.Error });
        if (parsed.Paid && (parsed.EventType is "checkout.session.completed" or "checkout.session.async_payment_succeeded") && parsed.OrderId.HasValue)
            await _wallet.CreditRechargeAsync(parsed.OrderId.Value, parsed.PaymentIntentId ?? string.Empty);
        return Ok(new { received = true });
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public class CreateRechargeRequest
{
    [Range(1, 10000)] public decimal Amount { get; set; }
    [Required, RegularExpression("^(usd|cny)$")] public string Currency { get; set; } = "usd";
}
