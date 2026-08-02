using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LLMPay.Domain.Entities;
using LLMPay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;
using LLMPay.Core.Services;
using Microsoft.Extensions.Configuration;

namespace LLMPay.Infrastructure.Services;

public record MockAccountResult(bool Valid, string? Email, string? AccountId, string Message);
public record MockRechargeView(Guid Id, string OrderNumber, string ContactEmail, string TargetEmail, string TargetName, string MockAccountId, string Plan, string PaymentProvider, decimal Amount, string Status, string? CheckoutSessionId, string? PaymentCodeUrl, decimal? PaymentAmountCny, string PaymentMode, DateTime? PaymentExpiresAt, string? SubscriptionId, string? RechargeCode, string? GuestAccessToken, bool AccountProvisioned, DateTime CreatedAt, DateTime? CompletedAt);

public partial class MockRechargeService
{
    private readonly LlmPayDbContext _db;
    private readonly IDataProtector _protector;
    private readonly IReadOnlyDictionary<string,INativePaymentGateway> _gateways;
    private readonly IConfiguration _configuration;
    public MockRechargeService(LlmPayDbContext db, IDataProtectionProvider protection, IEnumerable<INativePaymentGateway> gateways, IConfiguration configuration)
    {
        _db = db; _protector = protection.CreateProtector("LLMPay.MockRecharge.Code.v1"); _gateways=gateways.ToDictionary(x=>x.Provider,StringComparer.OrdinalIgnoreCase); _configuration=configuration;
    }

    /// <summary>只接受本系统定义的伪造格式。JWT、Bearer 和真实样式凭据均拒绝。</summary>
    public MockAccountResult ValidateTestToken(ReadOnlySpan<char> token)
    {
        var value = token.ToString().Trim();
        if (!value.StartsWith("test_session_", StringComparison.Ordinal) || value.Length is < 25 or > 180)
            return new(false, null, null, "仅接受本地生成的测试 Session 数据");
        if (value.Count(c => c == '.') >= 2 || value.Contains("Bearer ", StringComparison.OrdinalIgnoreCase) || !MockTokenRegex().IsMatch(value))
            return new(false, null, null, "检测到非测试 Token，已拒绝处理");

        // 格式：test_session_<email-safe-base64url>_<random>
        var payload = value[13..];
        var separator = payload.LastIndexOf('_');
        if (separator <= 0) return new(false, null, null, "测试 Token 格式不完整");
        try
        {
            var encoded = payload[..separator].Replace('-', '+').Replace('_', '/');
            encoded += new string('=', (4 - encoded.Length % 4) % 4);
            var email = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            if (!email.Contains('@') || email.Length > 256) return new(false, null, null, "测试账号邮箱无效");
            var accountId = $"mock_{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.ToLowerInvariant())))[..12].ToLowerInvariant()}";
            return new(true, email.ToLowerInvariant(), accountId, "测试账号验证成功");
        }
        catch { return new(false, null, null, "测试 Token 编码无效"); }
    }

    public async Task<MockRechargeView> CreateAsync(Guid? userId, string plan, string paymentProvider, string? contactEmail)
    {
        if (plan is not ("Plus" or "Credit100" or "Pro")) throw new ArgumentException("请选择有效充值档位");
        paymentProvider = paymentProvider.Trim().ToLowerInvariant();
        if (paymentProvider is not ("wechat" or "alipay")) throw new ArgumentException("请选择微信或支付宝");
        var user = userId.HasValue ? await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId) : null;
        contactEmail = (user?.Email ?? contactEmail ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(contactEmail) || !contactEmail.Contains('@') || contactEmail.Length > 256) throw new ArgumentException("请填写有效的联系邮箱");
        var guestToken = userId.HasValue ? null : Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var order = new MockRechargeOrder
        {
            Id = Guid.NewGuid(), OrderNumber = $"GPT{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{Random.Shared.Next(1000,9999)}",
            UserId = userId, ContactEmail = contactEmail, GuestAccessHash = guestToken is null ? null : HashRaw(guestToken), TargetEmail = "pending@mock.invalid", TargetName = "待确认", MockAccountId = "pending", Plan = plan, PaymentProvider = paymentProvider,
            Amount = plan switch { "Plus" => 20m, "Credit100" => 100m, _ => 200m }, Status = MockRechargeStatus.Created
        };
        _db.MockRechargeOrders.Add(order); await _db.SaveChangesAsync();
        return View(order, guestToken);
    }

    public async Task<MockRechargeView> CreateCheckoutAsync(Guid? userId, Guid id, string? guestToken)
    {
        var order = await Owned(userId, id, guestToken);
        if (order.Status != MockRechargeStatus.Created) throw new InvalidOperationException("当前状态不能创建 Checkout");
        var useGateway=bool.TryParse(_configuration["Payments:UseGatewaySandbox"],out var enabled)&&enabled;
        if(useGateway)
        {
            if(!_gateways.TryGetValue(order.PaymentProvider,out var gateway)||!gateway.IsConfigured)throw new InvalidOperationException($"{order.PaymentProvider} 支付环境尚未配置");
            var baseUrl=(_configuration["PublicBaseUrl"]??string.Empty).TrimEnd('/');
            if(string.IsNullOrWhiteSpace(baseUrl)||baseUrl.Contains("localhost",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("支付回调需要配置公网 HTTPS PublicBaseUrl");
            var amountSetting=_configuration[$"Payments:{(order.PaymentProvider=="wechat"?"WeChat":"Alipay")}:SandboxAmountCny"];
            var testAmount=decimal.TryParse(amountSetting,System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.InvariantCulture,out var configuredAmount)?configuredAmount:0.01m;
            var notifyUrl=$"{baseUrl}/api/payment-notify/{order.PaymentProvider}";
            var result=await gateway.CreateAsync(new(order.PaymentProvider,order.OrderNumber,$"{order.Plan} 充值测试",testAmount,notifyUrl));
            if(!result.Success)throw new InvalidOperationException(result.Error??"支付下单失败");
            order.MockCheckoutSessionId=result.GatewayTradeNo;order.PaymentCodeUrl=result.CodeUrl;order.PaymentAmountCny=testAmount;order.PaymentMode=result.Mode;order.PaymentExpiresAt=DateTime.UtcNow.AddMinutes(15);
        }
        else { order.MockCheckoutSessionId = $"cs_test_local_{Guid.NewGuid():N}"; order.PaymentMode="local"; order.PaymentAmountCny=null; }
        order.Status = MockRechargeStatus.CheckoutCreated; await _db.SaveChangesAsync(); return View(order);
    }

    public async Task<MockRechargeView> ConfirmPaymentAsync(Guid? userId, Guid id, string? guestToken)
    {
        var order = await Owned(userId, id, guestToken);
        if (order.Status != MockRechargeStatus.CheckoutCreated) throw new InvalidOperationException("尚未创建测试收银台");
        var rechargeCode = RechargeCardService.GenerateCode();
        order.RechargeCodeHash = Hash(rechargeCode);
        order.RechargeCodeSuffix = rechargeCode[^4..];
        order.RechargeCodeProtected = _protector.Protect(rechargeCode);
        order.Status = MockRechargeStatus.PaymentConfirmed; await _db.SaveChangesAsync(); return View(order);
    }

    public async Task<bool> ConfirmGatewayPaymentAsync(string orderNumber,string gatewayTradeNo,decimal? amountCny)
    {
        var order=await _db.MockRechargeOrders.FirstOrDefaultAsync(x=>x.OrderNumber==orderNumber);
        if(order is null)return false;
        if(order.Status is MockRechargeStatus.PaymentConfirmed or MockRechargeStatus.CardVerified or MockRechargeStatus.AccountVerified or MockRechargeStatus.AccountConfirmed or MockRechargeStatus.SubscriptionActivated)return true;
        if(order.Status!=MockRechargeStatus.CheckoutCreated)return false;
        if(order.PaymentAmountCny.HasValue&&(!amountCny.HasValue||order.PaymentAmountCny.Value!=amountCny.Value))return false;
        var rechargeCode=RechargeCardService.GenerateCode();order.RechargeCodeHash=Hash(rechargeCode);order.RechargeCodeSuffix=rechargeCode[^4..];order.RechargeCodeProtected=_protector.Protect(rechargeCode);order.PaymentGatewayTradeNo=gatewayTradeNo;order.Status=MockRechargeStatus.PaymentConfirmed;await _db.SaveChangesAsync();return true;
    }

    public async Task<MockRechargeView> VerifyCardAsync(Guid? userId, Guid id, string? guestToken, string rechargeCode)
    {
        var order = await Owned(userId, id, guestToken);
        if (order.Status != MockRechargeStatus.PaymentConfirmed) throw new InvalidOperationException("请先完成 Mock 支付");
        if (string.IsNullOrWhiteSpace(order.RechargeCodeHash) || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(order.RechargeCodeHash), Encoding.ASCII.GetBytes(Hash(rechargeCode))))
            throw new ArgumentException("充值卡密不正确");
        order.Status = MockRechargeStatus.CardVerified;
        await _db.SaveChangesAsync();
        return View(order);
    }

    public async Task<MockRechargeView> VerifyAccountAsync(Guid? userId, Guid id, string? guestToken, string sessionJson)
    {
        var order = await Owned(userId, id, guestToken);
        if (order.Status != MockRechargeStatus.CardVerified) throw new InvalidOperationException("请先验证充值卡密");
        if (sessionJson.Length > 5000) throw new ArgumentException("Session JSON 内容过长");
        string token;
        string accountName;
        try
        {
            using var json = JsonDocument.Parse(sessionJson);
            token = json.RootElement.GetProperty("accessToken").GetString() ?? string.Empty;
            accountName = json.RootElement.TryGetProperty("user", out var user) && user.TryGetProperty("name", out var name)
                ? (name.GetString() ?? "ChatGPT 用户").Trim()
                : "ChatGPT 用户";
        }
        catch { throw new ArgumentException("请输入有效的测试 Session JSON"); }
        var account = ValidateTestToken(token.AsSpan());
        if (!account.Valid) throw new ArgumentException(account.Message);
        order.TargetEmail = account.Email!; order.TargetName = accountName[..Math.Min(accountName.Length, 100)]; order.MockAccountId = account.AccountId!; order.Status = MockRechargeStatus.AccountVerified;
        await _db.SaveChangesAsync(); return View(order);
    }

    public async Task<MockRechargeView> ConfirmAccountAsync(Guid? userId, Guid id, string? guestToken)
    {
        var order = await Owned(userId, id, guestToken);
        if (order.Status != MockRechargeStatus.AccountVerified) throw new InvalidOperationException("充值账号尚未验证");
        order.Status = MockRechargeStatus.AccountConfirmed;
        await _db.SaveChangesAsync(); return View(order);
    }

    public async Task<MockRechargeView> ActivateAsync(Guid? userId, Guid id, string? guestToken)
    {
        var order = await Owned(userId, id, guestToken);
        if (order.Status != MockRechargeStatus.AccountConfirmed) throw new InvalidOperationException("请先确认充值账号");
        order.MockSubscriptionId = $"sub_test_mock_{Guid.NewGuid():N}"; order.Status = MockRechargeStatus.SubscriptionActivated; order.CompletedAt = DateTime.UtcNow;
        if (!order.UserId.HasValue)
        {
            var account = await _db.Users.FirstOrDefaultAsync(x => x.Email == order.ContactEmail);
            var isNew = account is null;
            var setupToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            if (account is null)
            {
                account = new User { Id = Guid.NewGuid(), Email = order.ContactEmail, Nickname = order.ContactEmail.Split('@')[0], PasswordHash = BCrypt.Net.BCrypt.HashPassword(Convert.ToHexString(RandomNumberGenerator.GetBytes(32))), Status = UserStatus.Active, EmailVerified = false };
                account.PasswordSetupTokenHash = HashRaw(setupToken); account.PasswordSetupExpiresAt = DateTime.UtcNow.AddHours(24);
                _db.Users.Add(account);
            }
            order.UserId = account.Id; order.AccountProvisioned = isNew;
            _db.EmailOutbox.Add(new EmailOutbox { Id = Guid.NewGuid(), ToEmail = order.ContactEmail, Subject = isNew ? "设置你的 LLMPay 登录密码" : "LLMPay 充值订单已完成", Body = isNew ? $"你的订单 {order.OrderNumber} 已完成。请在 24 小时内设置密码：http://127.0.0.1:5173/set-password?token={setupToken}&email={Uri.EscapeDataString(order.ContactEmail)}" : $"你的订单 {order.OrderNumber} 已完成，可登录账户查看。" });
        }
        await _db.SaveChangesAsync(); return View(order);
    }

    public async Task<MockRechargeView?> GetAsync(Guid? userId, Guid id, string? guestToken)
    {
        var order = await _db.MockRechargeOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (order is null || !CanAccess(order, userId, guestToken)) return null;
        return View(order);
    }

    public Task<Guid?> FindIdByOrderNumberAsync(string orderNumber) => _db.MockRechargeOrders.AsNoTracking().Where(x=>x.OrderNumber==orderNumber).Select(x=>(Guid?)x.Id).FirstOrDefaultAsync();

    public async Task<IReadOnlyList<MockRechargeView>> ListAsync(Guid userId)
    {
        var orders = await _db.MockRechargeOrders.AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync();
        return orders.Select(x => View(x)).ToList();
    }

    public async Task<MockRechargeView> CancelAsync(Guid? userId, Guid id, string? guestToken)
    {
        var order = await Owned(userId, id, guestToken);
        if (order.Status is not (MockRechargeStatus.Created or MockRechargeStatus.CheckoutCreated))
            throw new InvalidOperationException("订单已付款，不能取消；可在订单中心继续完成充值");
        order.Status = MockRechargeStatus.Cancelled;
        await _db.SaveChangesAsync();
        return View(order);
    }

    private async Task<MockRechargeOrder> Owned(Guid? userId, Guid id, string? guestToken)
    {
        var order = await _db.MockRechargeOrders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null || !CanAccess(order, userId, guestToken)) throw new KeyNotFoundException("订单不存在");
        return order;
    }
    private static bool CanAccess(MockRechargeOrder order, Guid? userId, string? guestToken)
    {
        if (userId.HasValue && order.UserId == userId) return true;
        return !string.IsNullOrWhiteSpace(guestToken) && !string.IsNullOrWhiteSpace(order.GuestAccessHash) && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(order.GuestAccessHash), Encoding.ASCII.GetBytes(HashRaw(guestToken)));
    }
    private MockRechargeView View(MockRechargeOrder x, string? guestAccessToken = null)
    {
        string? code = null;
        if ((x.Status is MockRechargeStatus.PaymentConfirmed or MockRechargeStatus.CardVerified or MockRechargeStatus.AccountVerified or MockRechargeStatus.AccountConfirmed or MockRechargeStatus.SubscriptionActivated) && !string.IsNullOrEmpty(x.RechargeCodeProtected)) code = _protector.Unprotect(x.RechargeCodeProtected);
        return new(x.Id, x.OrderNumber, x.ContactEmail, x.TargetEmail, x.TargetName, x.MockAccountId, x.Plan, x.PaymentProvider, x.Amount, x.Status.ToString(), x.MockCheckoutSessionId, x.PaymentCodeUrl, x.PaymentAmountCny, x.PaymentMode, x.PaymentExpiresAt, x.MockSubscriptionId, code, guestAccessToken, x.AccountProvisioned, x.CreatedAt, x.CompletedAt);
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(RechargeCardService.Normalize(value)))).ToLowerInvariant();
    private static string HashRaw(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    [GeneratedRegex("^test_session_[A-Za-z0-9_-]+_[A-Za-z0-9]{8,64}$", RegexOptions.CultureInvariant)] private static partial Regex MockTokenRegex();
}
