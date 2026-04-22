namespace Invest.Domain.Enums;

public enum AlertType
{
    Deviation, // Desvio extraordinário
    RankingChange, // Ativo saiu do ranking
    Rebalancing, // Hora de rebalancear
    Milestone // Atingiu milestone (ex: 100k)
}
