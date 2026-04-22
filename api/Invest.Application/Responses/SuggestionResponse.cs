namespace Invest.Application.Responses;

public record SuggestionResponse(
    Guid UserId,
    string SubEstrategiaAcoes,
    string SubEstrategiaFiis,
    List<SuggestionAssetResponse> AcoesRec,
    List<SuggestionAssetResponse> FiisRec
);

public record SuggestionAssetResponse(
    int Posicao,
    string Ticker,
    string Nome,
    decimal ScoreTotal,
    string? Justificativa
);
