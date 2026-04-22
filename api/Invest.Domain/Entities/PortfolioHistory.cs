using System.Text.Json;

namespace Invest.Domain.Entities;

public class PortfolioHistory
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime Data { get; private set; }
    public decimal ValorTotal { get; private set; }
    public decimal RentabilidadeNoDia { get; private set; }
    public decimal RentabilidadeAcumulada { get; private set; }
    public decimal DistanciaMeta { get; private set; }
    public string AlocacaoRealJson { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private PortfolioHistory() { }

    public static PortfolioHistory Create(
        Guid userId,
        DateTime data,
        decimal valorTotal,
        decimal rentabilidadeNoDia,
        decimal rentabilidadeAcumulada,
        decimal distanciaMeta,
        Dictionary<string, decimal> alocacaoReal)
    {
        return new PortfolioHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Data = data.Date,
            ValorTotal = valorTotal,
            RentabilidadeNoDia = rentabilidadeNoDia,
            RentabilidadeAcumulada = rentabilidadeAcumulada,
            DistanciaMeta = distanciaMeta,
            AlocacaoRealJson = JsonSerializer.Serialize(alocacaoReal),
            CreatedAt = DateTime.UtcNow
        };
    }

    public Dictionary<string, decimal> GetAlocacaoReal() =>
        JsonSerializer.Deserialize<Dictionary<string, decimal>>(AlocacaoRealJson) ?? new();
}
