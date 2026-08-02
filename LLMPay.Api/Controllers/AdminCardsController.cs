using LLMPay.Domain.Entities;
using LLMPay.Infrastructure.Data;
using LLMPay.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LLMPay.Api.Controllers;

[ApiController]
[Route("api/admin/cards")]
[Authorize(Roles = "Admin")]
public class AdminCardsController : ControllerBase
{
    private readonly LlmPayDbContext _db;
    private readonly RechargeCardService _cards;
    public AdminCardsController(LlmPayDbContext db, RechargeCardService cards) { _db = db; _cards = cards; }

    [HttpPost]
    public async Task<IActionResult> Generate(GenerateCardsRequest request)
    {
        try { return Ok(await _cards.GenerateAsync(request.Credits, request.Count, request.BatchName, request.ValidDays)); }
        catch (ArgumentOutOfRangeException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] RechargeCardStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.RechargeCards.AsNoTracking().Include(x => x.RedeemedByUser).AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.Id, maskedCode = $"••••-••••-••••-{x.CodeSuffix}", x.Credits, x.Currency, status = x.Status.ToString(), x.BatchName, x.CreatedAt, x.ExpiresAt, x.RedeemedAt, redeemedBy = x.RedeemedByUser == null ? null : x.RedeemedByUser.Email }).ToListAsync();
        return Ok(new { items, total, page, pageSize });
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, SetCardStatusRequest request)
    {
        var card = await _db.RechargeCards.FindAsync(id); if (card is null) return NotFound();
        if (card.Status == RechargeCardStatus.Redeemed) return Conflict(new { message = "已核销卡密不可修改" });
        if (request.Status is not (RechargeCardStatus.Active or RechargeCardStatus.Disabled)) return BadRequest();
        card.Status = request.Status; await _db.SaveChangesAsync(); return NoContent();
    }
}

public record GenerateCardsRequest(decimal Credits, int Count, string? BatchName, int ValidDays = 365);
public record SetCardStatusRequest(RechargeCardStatus Status);
