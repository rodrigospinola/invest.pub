namespace Invest.Application.Commands.Profile;

public record ComparePortfolioCommand(
    Guid UserId,
    Dictionary<string, decimal> CarteiraAtual
);
