using Microsoft.EntityFrameworkCore;
using Invest.Domain.Entities;
using Invest.Domain.Interfaces;
using Invest.Infrastructure.Data;

namespace Invest.Infrastructure.Repositories;

public class PortfolioHistoryRepository : IPortfolioHistoryRepository
{
    private readonly AppDbContext _context;

    public PortfolioHistoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PortfolioHistory history)
    {
        await _context.PortfolioHistories.AddAsync(history);
        await _context.SaveChangesAsync();
    }

    public async Task<List<PortfolioHistory>> GetByUserIdAsync(Guid userId, int lastDays = 30)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-lastDays);
        return await _context.PortfolioHistories
            .Where(x => x.UserId == userId && x.Data >= startDate)
            .OrderBy(x => x.Data)
            .ToListAsync();
    }

    public async Task<PortfolioHistory?> GetLatestByUserIdAsync(Guid userId)
    {
        return await _context.PortfolioHistories
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Data)
            .FirstOrDefaultAsync();
    }
}
