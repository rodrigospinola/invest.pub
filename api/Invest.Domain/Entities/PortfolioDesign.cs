using Invest.Domain.Enums;

namespace Invest.Domain.Entities;

public class PortfolioDesign
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid BatchRunId { get; private set; }
    public StatusPortfolio Status { get; private set; }
    public decimal ValorTotal { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PortfolioDesign() { }

    public static PortfolioDesign Create(Guid userId, Guid batchRunId, decimal valorTotal)
    {
        return new PortfolioDesign
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BatchRunId = batchRunId,
            Status = StatusPortfolio.Pendente,
            ValorTotal = valorTotal,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Confirmar()
    {
        Status = StatusPortfolio.Confirmado;
    }

    public void Cancelar()
    {
        Status = StatusPortfolio.Cancelado;
    }
}
