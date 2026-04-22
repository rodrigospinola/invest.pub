using Microsoft.EntityFrameworkCore;
using Invest.Domain.Entities;
using Invest.Domain.Interfaces;
using Invest.Infrastructure.Data;

namespace Invest.Infrastructure.Repositories;

public class AssetHistoryRepository : IAssetHistoryRepository
{
    private readonly AppDbContext _context;

    public AssetHistoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AssetHistory history)
    {
        await _context.AssetHistories.AddAsync(history);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AssetHistory>> GetByTickerAsync(string ticker, int lastDays = 30)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-lastDays);
        return await _context.AssetHistories
            .Where(x => x.Ticker == ticker && x.Data >= startDate)
            .OrderBy(x => x.Data)
            .ToListAsync();
    }
}
