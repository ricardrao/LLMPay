using LLMPay.Domain.Entities;
using LLMPay.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LLMPay.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly LlmPayDbContext _db;
    public AdminController(LlmPayDbContext db) => _db = db;

    [HttpGet("overview")]
    public async Task<IActionResult> Overview()
    {
        var completed = _db.PaymentOrders.Where(x => x.Status == OrderStatus.Completed);
        var recent = await _db.PaymentOrders.AsNoTracking().Include(x => x.User)
            .OrderByDescending(x => x.CreatedAt).Take(6)
            .Select(x => new { x.Id, x.OrderNumber, userEmail = x.User.Email, x.Amount, x.Currency, status = x.Status.ToString(), x.CreatedAt })
            .ToListAsync();
        return Ok(new
        {
            totalUsers = await _db.Users.CountAsync(),
            activeUsers = await _db.Users.CountAsync(x => x.Status == UserStatus.Active),
            totalOrders = await _db.PaymentOrders.CountAsync(),
            pendingOrders = await _db.PaymentOrders.CountAsync(x => x.Status == OrderStatus.Pending || x.Status == OrderStatus.Processing),
            completedRevenue = await completed.SumAsync(x => (decimal?)x.Amount) ?? 0,
            creditedBalance = await _db.Users.SumAsync(x => (decimal?)x.Balance) ?? 0,
            recentOrders = recent
        });
    }

    [HttpGet("orders")]
    public async Task<IActionResult> Orders([FromQuery] string? q, [FromQuery] OrderStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.PaymentOrders.AsNoTracking().Include(x => x.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.OrderNumber.Contains(q) || x.User.Email.Contains(q));
        if (status.HasValue) query = query.Where(x => x.Status == status);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.Id, x.OrderNumber, x.UserId, userEmail = x.User.Email, x.Amount, x.Currency, x.Credits, status = x.Status.ToString(), x.StripeSessionId, x.CreatedAt, x.CompletedAt, x.ErrorMessage })
            .ToListAsync();
        return Ok(new { items, total, page, pageSize });
    }

    [HttpPost("orders/{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id)
    {
        var order = await _db.PaymentOrders.FindAsync(id);
        if (order is null) return NotFound();
        if (order.Status is not (OrderStatus.Pending or OrderStatus.Processing)) return Conflict(new { message = "Only pending orders can be cancelled." });
        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery] string? q, [FromQuery] UserStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Email.Contains(q) || (x.Nickname != null && x.Nickname.Contains(q)));
        if (status.HasValue) query = query.Where(x => x.Status == status);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.Id, x.Email, x.Nickname, x.Balance, role = x.Role.ToString(), status = x.Status.ToString(), x.EmailVerified, x.CreatedAt, x.LastLoginAt })
            .ToListAsync();
        return Ok(new { items, total, page, pageSize });
    }

    [HttpPatch("users/{id:guid}/status")]
    public async Task<IActionResult> SetUserStatus(Guid id, ChangeUserStatusRequest request)
    {
        var currentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (id == currentId && request.Status != UserStatus.Active) return BadRequest(new { message = "You cannot suspend your own account." });
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();
        user.Status = request.Status;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record ChangeUserStatusRequest(UserStatus Status);
