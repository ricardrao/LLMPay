using Serilog;
using Microsoft.EntityFrameworkCore;
using LLMPay.Infrastructure.Data;
using LLMPay.Infrastructure.Repositories;
using LLMPay.Core.Interfaces;
using LLMPay.Core.Services;
using LLMPay.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using LLMPay.Domain.Entities;
using LLMPay.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 配置Serilog日志
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// 添加服务
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtSecret = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey must be configured.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    });
builder.Services.AddAuthorization();

// 配置数据库连接
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = "Host=localhost;Port=5432;Database=llmpay;Username=postgres;Password=postgres";
}

builder.Services.AddDbContext<LlmPayDbContext>(options =>
    options.UseNpgsql(connectionString));

// 注册仓储
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPaymentOrderRepository, PaymentOrderRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<RechargeCardService>();
builder.Services.AddScoped<CardPurchaseService>();
builder.Services.AddScoped<MockRechargeService>();
builder.Services.AddDataProtection();

// 注册服务
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPaymentOrderService, PaymentOrderService>();
builder.Services.AddHttpClient<IRechargePaymentService, StripeCheckoutService>(client =>
{
    client.BaseAddress = new Uri("https://api.stripe.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// 配置HttpClient
// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Security:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader();
    });
});

// 配置API版本
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
});

var app = builder.Build();

// 配置中间件
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 数据库迁移（非致命，数据库未启动时仅警告）
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LlmPayDbContext>();
    try
    {
        Log.Information("Applying database migrations...");
        dbContext.Database.Migrate();
        var adminEmail = builder.Configuration["Admin:BootstrapEmail"]?.Trim().ToLowerInvariant();
        var adminPassword = builder.Configuration["Admin:BootstrapPassword"];
        if (app.Environment.IsDevelopment() && !string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var admin = dbContext.Users.FirstOrDefault(x => x.Email == adminEmail);
            if (admin is null)
            {
                admin = new User
                {
                    Id = Guid.NewGuid(), Email = adminEmail, Nickname = "平台管理员",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    Role = UserRole.Admin, Status = UserStatus.Active, EmailVerified = true
                };
                dbContext.Users.Add(admin);
            }
            else admin.Role = UserRole.Admin;
            dbContext.SaveChanges();
        }
        Log.Information("Database migrations completed successfully.");
    }
    catch (Exception ex)
    {
        Log.Warning("Database not available: {Message}. API will start without database.", ex.Message);
    }
}

app.Run();
