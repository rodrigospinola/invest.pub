using Invest.Domain.Entities;

namespace Invest.Domain.Interfaces;

public interface IPortfolioHistoryRepository
{
    Task AddAsync(PortfolioHistory history);
    Task<List<PortfolioHistory>> GetByUserIdAsync(Guid userId, int lastDays = 30);
    Task<PortfolioHistory?> GetLatestByUserIdAsync(Guid userId);
}
