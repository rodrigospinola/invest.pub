using Microsoft.EntityFrameworkCore;
using Invest.Domain.Entities;
using Invest.Domain.Enums;
using Invest.Domain.Interfaces;
using Invest.Infrastructure.Data;

namespace Invest.Infrastructure.Repositories;

public class AlertRepository : IAlertRepository
{
    private readonly AppDbContext _context;

    public AlertRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Alert alert)
    {
        await _context.Alerts.AddAsync(alert);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Alert>> GetUnreadByUserIdAsync(Guid userId)
    {
        return await _context.Alerts
            .Where(x => x.UserId == userId && x.Status == AlertStatus.Unread)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<Alert?> GetByIdAsync(Guid id)
    {
        return await _context.Alerts.FindAsync(id);
    }

    public async Task UpdateAsync(Alert alert)
    {
        _context.Alerts.Update(alert);
        await _context.SaveChangesAsync();
    }
}
