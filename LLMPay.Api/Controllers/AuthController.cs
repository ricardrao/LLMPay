using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LLMPay.Core.Services;
using LLMPay.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;

namespace LLMPay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IConfiguration _configuration;

    public AuthController(IUserService users, IConfiguration configuration)
    {
        _users = users;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _users.RegisterAsync(request.Email, request.Password, request.Nickname);
        return result.Success ? Ok(new { result.Message, userId = result.User!.Id }) : BadRequest(new { result.Message });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _users.LoginAsync(request.Email, request.Password);
        if (!result.Success) return Unauthorized(new { result.Message });
        return Ok(new { token = CreateToken(result.User!), user = ToUserResponse(result.User!) });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _users.GetUserAsync(id);
        return user is null ? Unauthorized() : Ok(ToUserResponse(user));
    }

    private string CreateToken(User user)
    {
        var secret = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey is required.");
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(JwtRegisteredClaimNames.Email, user.Email), new Claim(ClaimTypes.Role, user.Role.ToString()) };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_configuration["Jwt:Issuer"], _configuration["Jwt:Audience"], claims,
            expires: DateTime.UtcNow.AddMinutes(_configuration.GetValue("Jwt:ExpireMinutes", 60)), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static object ToUserResponse(User user) => new { user.Id, user.Email, user.Nickname, user.Balance, role = user.Role.ToString(), status = user.Status.ToString(), user.EmailVerified, user.CreatedAt };
}

public class RegisterRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;
    [MaxLength(100)] public string? Nickname { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}
