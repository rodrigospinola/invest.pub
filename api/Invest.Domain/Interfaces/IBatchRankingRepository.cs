using Invest.Domain.Entities;

namespace Invest.Domain.Interfaces;

public interface IBatchRankingRepository
{
    Task<List<BatchRanking>> GetLatestBySubEstrategiaAsync(string subEstrategia, int limit = 20);
    Task<BatchRun?> GetLatestRunAsync();
    Task AddRunAsync(BatchRun run);
    Task UpdateRunAsync(BatchRun run);
    Task AddRankingsAsync(List<BatchRanking> rankings);
}
