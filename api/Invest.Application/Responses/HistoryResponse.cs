namespace Invest.Application.Responses;

public record HistoryPointResponse(
    DateTime Data,
    decimal ValorTotal,
    decimal RentabilidadeAcumulada
);

public record HistoryResponse(
    List<HistoryPointResponse> Pontos,
    List<BenchmarkResponse> Benchmarks
);
