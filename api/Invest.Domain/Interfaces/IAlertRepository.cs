using Invest.Domain.Entities;

namespace Invest.Domain.Interfaces;

public interface IAlertRepository
{
    Task AddAsync(Alert alert);
    Task<List<Alert>> GetUnreadByUserIdAsync(Guid userId);
    Task<Alert?> GetByIdAsync(Guid id);
    Task UpdateAsync(Alert alert);
}
