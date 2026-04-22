using Invest.Domain.Entities;

namespace Invest.Domain.Interfaces;

public interface IUserSubStrategyRepository
{
    Task<UserSubStrategy?> GetByUserIdAsync(Guid userId);
    Task AddAsync(UserSubStrategy subStrategy);
    Task UpdateAsync(UserSubStrategy subStrategy);
    Task DeleteByUserIdAsync(Guid userId);
}
