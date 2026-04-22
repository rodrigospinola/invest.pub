using Microsoft.EntityFrameworkCore;
using Invest.Domain.Entities;
using Invest.Domain.Interfaces;
using Invest.Infrastructure.Data;

namespace Invest.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetByEmailAsync(string email) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower());

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken) =>
        await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> EmailExistsAsync(string email) =>
        await _context.Users.AnyAsync(u => u.Email == email.ToLower());
}
