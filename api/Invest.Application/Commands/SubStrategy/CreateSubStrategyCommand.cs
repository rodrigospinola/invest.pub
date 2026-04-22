namespace Invest.Application.Commands.SubStrategy;

public record CreateSubStrategyCommand(Guid UserId, string SubEstrategiaAcoes, string SubEstrategiaFiis);
