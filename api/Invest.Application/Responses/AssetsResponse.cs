namespace Invest.Application.Responses;

public record AssetsResponse(List<AssetItemResponse> Ativos);

public record AssetItemResponse(
    Guid Id,
    string Ticker,
    string Nome,
    string Classe,
    decimal Quantidade,
    decimal PrecoMedio,
    string Origem,
    bool Ativo,
    DateTime CreatedAt
);
