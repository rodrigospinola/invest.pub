using Invest.Domain.Entities;

namespace Invest.Domain.Interfaces;

public interface IAssetHistoryRepository
{
    Task AddAsync(AssetHistory history);
    Task<List<AssetHistory>> GetByTickerAsync(string ticker, int lastDays = 30);
}
