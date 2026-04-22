namespace Invest.Application.Responses;

public record BenchmarkPointResponse(
    DateTime Data,
    decimal Valor,
    decimal VariacaoNoDia
);

public record BenchmarkResponse(
    string Nome,
    List<BenchmarkPointResponse> Pontos
);
