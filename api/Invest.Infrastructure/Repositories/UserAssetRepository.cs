using Microsoft.EntityFrameworkCore;
using Invest.Domain.Entities;
using Invest.Domain.Interfaces;
using Invest.Infrastructure.Data;

namespace Invest.Infrastructure.Repositories;

public class UserAssetRepository : IUserAssetRepository
{
    private readonly AppDbContext _context;

    public UserAssetRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserAsset>> GetByUserIdAsync(Guid userId) =>
        await _context.UserAssets
            .Where(a => a.UserId == userId)
            .ToListAsync();

    public async Task AddRangeAsync(List<UserAsset> assets)
    {
        await _context.UserAssets.AddRangeAsync(assets);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserAsset asset)
    {
        _context.UserAssets.Update(asset);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAllByUserIdAsync(Guid userId)
    {
        var assets = await _context.UserAssets.Where(a => a.UserId == userId).ToListAsync();
        if (assets.Count > 0)
        {
            _context.UserAssets.RemoveRange(assets);
            await _context.SaveChangesAsync();
        }
    }
}
