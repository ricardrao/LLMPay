using Microsoft.EntityFrameworkCore;
using LLMPay.Domain.Entities;

namespace LLMPay.Infrastructure.Repositories;

/// <summary>
/// 通用仓储基类
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class RepositoryBase<T> where T : class
{
    protected readonly Data.LlmPayDbContext Context;
    protected readonly DbSet<T> DbSet;

    protected RepositoryBase(Data.LlmPayDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await DbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        DbSet.Update(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task DeleteAsync(T entity)
    {
        DbSet.Remove(entity);
        await Context.SaveChangesAsync();
    }

    public virtual async Task<bool> AnyAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
    {
        return await DbSet.AnyAsync(predicate);
    }

    public virtual async Task<T?> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
    {
        return await DbSet.FirstOrDefaultAsync(predicate);
    }
}
