using Invest.Domain.Entities;

namespace Invest.Domain.Interfaces;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(Guid userId);
    Task AddAsync(UserProfile profile);
    Task UpdateAsync(UserProfile profile);
    Task DeleteByUserIdAsync(Guid userId);
}
