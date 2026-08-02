using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LLMPay.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LLMPay.Api.Controllers;

[ApiController]
[Route("api/cards")]
[Authorize]
public class CardsController : ControllerBase
{
    private readonly RechargeCardService _cards;
    public CardsController(RechargeCardService cards) => _cards = cards;

    [HttpPost("redeem")]
    public async Task<IActionResult> Redeem(RedeemCardRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _cards.RedeemAsync(userId, request.Code);
        return result.Success ? Ok(result) : BadRequest(new { result.Message });
    }

}

public record RedeemCardRequest([Required, MinLength(16)] string Code);
