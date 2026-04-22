using Microsoft.EntityFrameworkCore;
using Invest.Domain.Entities;
using Invest.Domain.Interfaces;
using Invest.Infrastructure.Data;

namespace Invest.Infrastructure.Repositories;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly AppDbContext _context;

    public UserProfileRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfile?> GetByUserIdAsync(Guid userId) =>
        await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

    public async Task AddAsync(UserProfile profile)
    {
        await _context.UserProfiles.AddAsync(profile);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserProfile profile)
    {
        _context.UserProfiles.Update(profile);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteByUserIdAsync(Guid userId)
    {
        var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile != null)
        {
            _context.UserProfiles.Remove(profile);
            await _context.SaveChangesAsync();
        }
    }
}
