using LLMPay.Domain.Entities;
using LLMPay.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace LLMPay.Core.Services;

public interface IUserService
{
    Task<(bool Success, string Message, User? User)> RegisterAsync(string email, string password, string? nickname);
    Task<(bool Success, string Message, User? User)> LoginAsync(string email, string password);
    Task<User?> GetUserAsync(Guid userId);
    Task<(bool Success, string Message)> UpdateBalanceAsync(Guid userId, decimal amount, bool isDeduction = false);
    Task<(bool Success, string Message)> UpdateUserStatusAsync(Guid userId, UserStatus status);
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, User? User)> RegisterAsync(string email, string password, string? nickname)
    {
        try
        {
            // 验证邮箱格式
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                return (false, "Invalid email format", null);

            // 验证密码强度
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                return (false, "Password must be at least 6 characters", null);

            // 检查邮箱是否已存在
            var emailExists = await _userRepository.EmailExistsAsync(email);
            if (emailExists)
                return (false, "Email already registered", null);

            // 创建新用户
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email.ToLower(),
                Nickname = nickname ?? email.Split('@')[0],
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Status = UserStatus.Active
            };

            var createdUser = await _userRepository.AddAsync(user);
            _logger.LogInformation("User registered: {Email}", email);

            return (true, "Registration successful", createdUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user: {Email}", email);
            return (false, "Registration failed", null);
        }
    }

    public async Task<(bool Success, string Message, User? User)> LoginAsync(string email, string password)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(email.ToLower());
            if (user == null)
                return (false, "Invalid email or password", null);

            // 验证密码
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return (false, "Invalid email or password", null);

            // 检查账户状态
            if (user.Status != UserStatus.Active)
                return (false, "Account is not active", null);

            // 更新最后登录时间
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User logged in: {Email}", email);

            return (true, "Login successful", user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in user: {Email}", email);
            return (false, "Login failed", null);
        }
    }

    public async Task<User?> GetUserAsync(Guid userId)
    {
        return await _userRepository.GetByIdAsync(userId);
    }

    public async Task<(bool Success, string Message)> UpdateBalanceAsync(Guid userId, decimal amount, bool isDeduction = false)
    {
        try
        {
            if (amount <= 0)
                return (false, "Amount must be greater than zero");

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return (false, "User not found");

            if (isDeduction)
            {
                if (user.Balance < amount)
                    return (false, "Insufficient balance");
                user.Balance -= amount;
            }
            else
            {
                user.Balance += amount;
            }

            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("User balance updated: UserId={UserId}, Amount={Amount}, IsDeduction={IsDeduction}", 
                userId, amount, isDeduction);

            return (true, "Balance updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating balance for user: {UserId}", userId);
            return (false, "Failed to update balance");
        }
    }

    public async Task<(bool Success, string Message)> UpdateUserStatusAsync(Guid userId, UserStatus status)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return (false, "User not found");

            user.Status = status;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User status updated: UserId={UserId}, Status={Status}", userId, status);

            return (true, "User status updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user status: {UserId}", userId);
            return (false, "Failed to update user status");
        }
    }
}
