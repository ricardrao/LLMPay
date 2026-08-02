using Microsoft.EntityFrameworkCore;
using LLMPay.Domain.Entities;
using LLMPay.Core.Interfaces;

namespace LLMPay.Infrastructure.Repositories;

public class UserRepository : RepositoryBase<User>, IUserRepository
{
    public UserRepository(Data.LlmPayDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await DbSet
            .Include(u => u.PaymentOrders)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await DbSet.AnyAsync(u => u.Email == email);
    }
}
