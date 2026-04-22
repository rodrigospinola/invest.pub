namespace Invest.Application.Responses;

public record B3ImportResultResponse
{
    public required List<ParsedAsset> ParsedAssets { get; init; }
    public required List<string> Errors { get; init; }
    public int TotalRowsProcessed { get; init; }
}

public record ParsedAsset
{
    public required string Ticker { get; init; }
    public required string Classe { get; init; }
    public decimal Quantidade { get; init; }
    public required string Instituicao { get; init; }
    public decimal? PrecoMedio { get; init; }
}
