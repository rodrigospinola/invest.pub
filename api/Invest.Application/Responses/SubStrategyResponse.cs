namespace Invest.Application.Responses;

public record SubStrategyResponse(
    Guid UserId,
    string SubEstrategiaAcoes,
    string SubEstrategiaFiis,
    DateTime CreatedAt
);
