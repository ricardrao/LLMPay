using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LLMPay.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LLMPay.Api.Controllers;

[ApiController]
[Route("api/mock-recharge")]
[AllowAnonymous]
public class MockRechargeController : ControllerBase
{
    private readonly MockRechargeService _service;
    private readonly IWebHostEnvironment _environment;
    public MockRechargeController(MockRechargeService service, IWebHostEnvironment environment) { _service = service; _environment = environment; }

    [HttpPost("token")]
    public IActionResult GenerateToken(GenerateMockTokenRequest request)
    {
        if (!_environment.IsDevelopment()) return NotFound();
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(request.Email.Trim().ToLowerInvariant())).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var random = Convert.ToHexString(RandomNumberGenerator.GetBytes(10)).ToLowerInvariant();
        var token = $"test_session_{encoded}_{random}";
        return Ok(new { sessionJson = System.Text.Json.JsonSerializer.Serialize(new { user = new { email = request.Email.Trim().ToLowerInvariant(), name = "Test ChatGPT User" }, accessToken = token, expires = DateTime.UtcNow.AddHours(1) }), test = true });
    }

    [HttpPost("orders")]
    public async Task<IActionResult> Create(CreateMockRechargeRequest request)
    {
        if (!_environment.IsDevelopment()) return NotFound();
        try { return Ok(await _service.CreateAsync(UserId(), request.Plan, request.PaymentProvider, request.ContactEmail)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("orders/{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        if (!_environment.IsDevelopment()) return NotFound();
        var result = await _service.GetAsync(UserId(), id, GuestToken()); return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("orders")]
    public async Task<IActionResult> List()
    {
        if (!_environment.IsDevelopment()) return NotFound();
        var userId = UserId(); if (!userId.HasValue) return Unauthorized();
        return Ok(await _service.ListAsync(userId.Value));
    }

    [HttpPost("orders/{id:guid}/checkout")]
    public async Task<IActionResult> Checkout(Guid id) => await Run(() => _service.CreateCheckoutAsync(UserId(), id, GuestToken()));

    [HttpPost("orders/{id:guid}/payment")]
    public async Task<IActionResult> Payment(Guid id) => await Run(() => _service.ConfirmPaymentAsync(UserId(), id, GuestToken()));

    [HttpPost("orders/{id:guid}/verify-card")]
    public async Task<IActionResult> VerifyCard(Guid id, VerifyRechargeCardRequest request)
    {
        if (!_environment.IsDevelopment()) return NotFound();
        try { return Ok(await _service.VerifyCardAsync(UserId(), id, GuestToken(), request.RechargeCode)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("orders/{id:guid}/verify-account")]
    public async Task<IActionResult> VerifyAccount(Guid id, VerifyMockAccountRequest request)
    {
        if (!_environment.IsDevelopment()) return NotFound();
        try { return Ok(await _service.VerifyAccountAsync(UserId(), id, GuestToken(), request.SessionJson)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("orders/{id:guid}/confirm-account")]
    public async Task<IActionResult> ConfirmAccount(Guid id) => await Run(() => _service.ConfirmAccountAsync(UserId(), id, GuestToken()));

    [HttpPost("orders/{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id) => await Run(() => _service.ActivateAsync(UserId(), id, GuestToken()));

    [HttpPost("orders/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id) => await Run(() => _service.CancelAsync(UserId(), id, GuestToken()));

    private async Task<IActionResult> Run(Func<Task<MockRechargeView>> action)
    {
        if (!_environment.IsDevelopment()) return NotFound();
        try { return Ok(await action()); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
    private Guid? UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private string? GuestToken() => Request.Headers["X-Order-Access"].FirstOrDefault();
}

public record GenerateMockTokenRequest([Required, EmailAddress] string Email);
public record CreateMockRechargeRequest([Required] string Plan, [Required] string PaymentProvider, [EmailAddress] string? ContactEmail);
public record VerifyRechargeCardRequest([Required] string RechargeCode);
public record VerifyMockAccountRequest([Required] string SessionJson);
