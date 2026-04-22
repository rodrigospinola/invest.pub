namespace Invest.Application.Responses;

public record PortfolioImportResponse(
    int TotalImportados,
    int Sugeridos,
    int Proprios,
    List<string> TickersFalharam
);
