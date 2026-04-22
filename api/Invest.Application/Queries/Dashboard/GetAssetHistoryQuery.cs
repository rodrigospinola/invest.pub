namespace Invest.Application.Queries.Dashboard;

public record GetAssetHistoryQuery(string Ticker, int LastDays = 30);

public record AssetHistoryResponse(
    DateTime Data,
    decimal PrecoFechamento,
    decimal DividendoNoDia
);
