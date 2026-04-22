using Microsoft.EntityFrameworkCore;
using Invest.Domain.Entities;
using Invest.Domain.Interfaces;
using Invest.Infrastructure.Data;

namespace Invest.Infrastructure.Repositories;

public class BenchmarkRepository : IBenchmarkRepository
{
    private readonly AppDbContext _context;

    public BenchmarkRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Benchmark benchmark)
    {
        await _context.Benchmarks.AddAsync(benchmark);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Benchmark>> GetByNameAsync(string nome, int lastDays = 30)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-lastDays);
        return await _context.Benchmarks
            .Where(x => x.Nome == nome && x.Data >= startDate)
            .OrderBy(x => x.Data)
            .ToListAsync();
    }
}
