namespace Invest.Application.Responses;

public record ComparisonItemResponse(
    string Classe,
    decimal Atual,
    decimal Recomendado,
    decimal Delta
);

public record ComparisonResponse(
    List<ComparisonItemResponse> Comparacao
);
