using System.Security.Claims;
using LLMPay.Domain.Entities;
using LLMPay.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LLMPay.Api.Controllers;

[ApiController]
[Route("api/card-purchases")]
[Authorize]
public class CardPurchasesController : ControllerBase
{
    private readonly CardPurchaseService _purchases;
    private readonly IWebHostEnvironment _environment;
    public CardPurchasesController(CardPurchaseService purchases, IWebHostEnvironment environment) { _purchases = purchases; _environment = environment; }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCardPurchaseRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _purchases.CreateAsync(userId, request.Credits, request.Provider, _environment.IsDevelopment()));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _purchases.GetAsync(userId, id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>开发环境模拟支付回调。生产环境不可用。</summary>
    [HttpPost("{id:guid}/simulate-payment")]
    public async Task<IActionResult> SimulatePayment(Guid id)
    {
        if (!_environment.IsDevelopment()) return NotFound();
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _purchases.GetAsync(userId, id);
        if (order is null) return NotFound();
        return Ok(await _purchases.ConfirmPaidAsync(id, $"DEMO-{Guid.NewGuid():N}"));
    }

    [AllowAnonymous]
    [HttpPost("callbacks/wechat")]
    public IActionResult WeChatCallback() => StatusCode(501, new { message = "微信支付商户证书尚未配置，拒绝未验证回调。" });

    [AllowAnonymous]
    [HttpPost("callbacks/alipay")]
    public IActionResult AlipayCallback() => StatusCode(501, new { message = "支付宝应用公钥尚未配置，拒绝未验证回调。" });
}

public record CreateCardPurchaseRequest(decimal Credits, PaymentProvider Provider);
