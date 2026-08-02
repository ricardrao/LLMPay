using Microsoft.EntityFrameworkCore;
using LLMPay.Domain.Entities;

namespace LLMPay.Infrastructure.Data;

/// <summary>
/// 数据库上下文 - PostgreSQL
/// </summary>
public class LlmPayDbContext : DbContext
{
    public LlmPayDbContext(DbContextOptions<LlmPayDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<PaymentOrder> PaymentOrders { get; set; } = null!;
    public DbSet<CreditTransaction> CreditTransactions { get; set; } = null!;
    public DbSet<RechargeCard> RechargeCards { get; set; } = null!;
    public DbSet<CardPurchaseOrder> CardPurchaseOrders { get; set; } = null!;
    public DbSet<MockRechargeOrder> MockRechargeOrders { get; set; } = null!;
    public DbSet<EmailOutbox> EmailOutbox { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置User实体
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Nickname).HasMaxLength(100);
            entity.Property(e => e.Balance).HasPrecision(18, 6);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Role).HasConversion<int>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'UTC'");
            
            // 导航属性配置
            entity.HasMany(e => e.PaymentOrders)
                .WithOne(po => po.User)
                .HasForeignKey(po => po.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.CreditTransactions)
                .WithOne(ct => ct.User)
                .HasForeignKey(ct => ct.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 配置PaymentOrder实体
        modelBuilder.Entity<PaymentOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.HasIndex(e => e.StripeSessionId);
            entity.HasIndex(e => e.StripePaymentIntentId);
            
            entity.Property(e => e.OrderNumber).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.PaymentMethod).HasConversion<int>();
            entity.Property(e => e.StripeSessionId).HasMaxLength(256);
            entity.Property(e => e.StripePaymentIntentId).HasMaxLength(256);
            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Credits).HasPrecision(18, 6);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.ClientIp).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'UTC'");
            entity.Property(e => e.PasswordSetupTokenHash).HasMaxLength(64);
        });

        modelBuilder.Entity<CreditTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReferenceId).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.Property(e => e.Amount).HasPrecision(18, 6);
            entity.Property(e => e.BalanceAfter).HasPrecision(18, 6);
            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.ReferenceId).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'UTC'");
        });

        modelBuilder.Entity<RechargeCard>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CodeHash).IsUnique();
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
            entity.Property(e => e.CodeHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.CodeSuffix).HasMaxLength(8).IsRequired();
            entity.Property(e => e.Credits).HasPrecision(18, 6);
            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.BatchName).HasMaxLength(100);
            entity.HasOne(e => e.RedeemedByUser)
                .WithMany(e => e.RedeemedCards)
                .HasForeignKey(e => e.RedeemedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CardPurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.Property(e => e.OrderNumber).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Credits).HasPrecision(18, 6);
            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Provider).HasConversion<int>();
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.ProviderTradeNumber).HasMaxLength(128);
            entity.Property(e => e.IssuedCardCodeProtected).HasMaxLength(2000);
            entity.HasOne(e => e.User).WithMany(e => e.CardPurchaseOrders).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.IssuedCard).WithMany().HasForeignKey(e => e.IssuedCardId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MockRechargeOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.Property(e => e.OrderNumber).HasMaxLength(64).IsRequired();
            entity.Property(e => e.TargetEmail).HasMaxLength(256).IsRequired();
            entity.Property(e => e.ContactEmail).HasMaxLength(256).IsRequired();
            entity.Property(e => e.GuestAccessHash).HasMaxLength(64);
            entity.Property(e => e.TargetName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.MockAccountId).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Plan).HasMaxLength(32).IsRequired();
            entity.Property(e => e.PaymentProvider).HasMaxLength(16).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.MockCheckoutSessionId).HasMaxLength(128);
            entity.Property(e => e.MockSubscriptionId).HasMaxLength(128);
            entity.Property(e => e.RechargeCodeHash).HasMaxLength(64);
            entity.Property(e => e.RechargeCodeSuffix).HasMaxLength(8);
            entity.Property(e => e.RechargeCodeProtected).HasMaxLength(2000);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.HasOne(e => e.User).WithMany(e => e.MockRechargeOrders).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        });
        modelBuilder.Entity<EmailOutbox>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ToEmail).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Subject).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Body).HasMaxLength(4000).IsRequired();
        });
    }
}
