using Microsoft.EntityFrameworkCore;
using Invest.Domain.Entities;
using Invest.Domain.Interfaces;
using Invest.Infrastructure.Data;

namespace Invest.Infrastructure.Repositories;

public class UserSubStrategyRepository : IUserSubStrategyRepository
{
    private readonly AppDbContext _context;

    public UserSubStrategyRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserSubStrategy?> GetByUserIdAsync(Guid userId) =>
        await _context.UserSubStrategies.FirstOrDefaultAsync(s => s.UserId == userId);

    public async Task AddAsync(UserSubStrategy subStrategy)
    {
        await _context.UserSubStrategies.AddAsync(subStrategy);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserSubStrategy subStrategy)
    {
        _context.UserSubStrategies.Update(subStrategy);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteByUserIdAsync(Guid userId)
    {
        var sub = await _context.UserSubStrategies.FirstOrDefaultAsync(s => s.UserId == userId);
        if (sub != null)
        {
            _context.UserSubStrategies.Remove(sub);
            await _context.SaveChangesAsync();
        }
    }
}
