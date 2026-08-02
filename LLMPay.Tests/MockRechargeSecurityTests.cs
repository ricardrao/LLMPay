using System.Text;
using LLMPay.Infrastructure.Data;
using LLMPay.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;

namespace LLMPay.Tests;

public class MockRechargeSecurityTests
{
    private static MockRechargeService Service()
    {
        var options = new DbContextOptionsBuilder<LlmPayDbContext>().UseNpgsql("Host=localhost;Database=unused").Options;
        return new MockRechargeService(new LlmPayDbContext(options), DataProtectionProvider.Create("LLMPay.Tests"));
    }

    [Fact]
    public void ValidMockToken_IsAcceptedAndDecodesEmail()
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("buyer@example.com")).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var result = Service().ValidateTestToken($"test_session_{encoded}_12345678".AsSpan());
        Assert.True(result.Valid);
        Assert.Equal("buyer@example.com", result.Email);
        Assert.StartsWith("mock_", result.AccountId);
    }

    [Theory]
    [InlineData("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.signature")]
    [InlineData("Bearer real-looking-token")]
    [InlineData("sess-real-token")]
    [InlineData("test_session_bad")]
    public void NonMockCredential_IsRejected(string token)
    {
        Assert.False(Service().ValidateTestToken(token.AsSpan()).Valid);
    }
}
