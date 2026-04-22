using Invest.Domain.Entities;

namespace Invest.Domain.Interfaces;

public interface IBenchmarkRepository
{
    Task AddAsync(Benchmark benchmark);
    Task<List<Benchmark>> GetByNameAsync(string nome, int lastDays = 30);
}
