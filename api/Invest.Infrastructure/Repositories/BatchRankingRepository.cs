using Microsoft.EntityFrameworkCore;
using Invest.Domain.Entities;
using Invest.Domain.Interfaces;
using Invest.Infrastructure.Data;

namespace Invest.Infrastructure.Repositories;

public class BatchRankingRepository : IBatchRankingRepository
{
    private readonly AppDbContext _context;

    public BatchRankingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<BatchRanking>> GetLatestBySubEstrategiaAsync(string subEstrategia, int limit = 20) =>
        await _context.BatchRankings
            .Where(r => r.SubEstrategia == subEstrategia)
            .OrderByDescending(r => r.DataRanking)
            .ThenBy(r => r.Posicao)
            .Take(limit)
            .ToListAsync();

    public async Task<BatchRun?> GetLatestRunAsync() =>
        await _context.BatchRuns
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task AddRunAsync(BatchRun run)
    {
        await _context.BatchRuns.AddAsync(run);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRunAsync(BatchRun run)
    {
        _context.BatchRuns.Update(run);
        await _context.SaveChangesAsync();
    }

    public async Task AddRankingsAsync(List<BatchRanking> rankings)
    {
        await _context.BatchRankings.AddRangeAsync(rankings);
        await _context.SaveChangesAsync();
    }
}
