namespace Invest.Application.Responses;

public record RankingResponse(
    string SubEstrategia,
    DateOnly DataRanking,
    List<RankingItemResponse> Itens
);

public record RankingItemResponse(
    int Posicao,
    string Ticker,
    string Nome,
    decimal ScoreTotal,
    decimal ScoreQuantitativo,
    decimal ScoreQualitativo,
    string? Justificativa,
    object? Indicadores,
    bool EntrouHoje,
    bool SaiuHoje
);
