namespace Invest.Application.Responses;

public record DashboardResponse(
    decimal ValorTotal,
    decimal RentabilidadeNoDia,
    decimal RentabilidadeAcumulada,
    decimal DistanciaMeta,
    decimal PercentualMeta,
    List<DeviationResponse> Alocacoes,
    List<AlertResponse> AlertasRecentes
);
