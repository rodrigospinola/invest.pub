namespace Invest.Application.Commands.Portfolio;

public record ImportPortfolioAsset(string Ticker, string Nome, string Classe, decimal Quantidade, decimal PrecoMedio);

public record ImportPortfolioCommand(Guid UserId, List<ImportPortfolioAsset> Ativos);
