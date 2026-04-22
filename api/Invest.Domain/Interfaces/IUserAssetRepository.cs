using Invest.Domain.Entities;

namespace Invest.Domain.Interfaces;

public interface IUserAssetRepository
{
    Task<List<UserAsset>> GetByUserIdAsync(Guid userId);
    Task AddRangeAsync(List<UserAsset> assets);
    Task UpdateAsync(UserAsset asset);
    Task DeleteAllByUserIdAsync(Guid userId);
}
