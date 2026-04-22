namespace Invest.Domain.Entities;

public class BatchRanking
{
    public Guid Id { get; private set; }
    public Guid BatchRunId { get; private set; }
    public string SubEstrategia { get; private set; } = string.Empty;
    public string Ticker { get; private set; } = string.Empty;
    public string Nome { get; private set; } = string.Empty;
    public int Posicao { get; private set; }
    public decimal ScoreTotal { get; private set; }
    public decimal ScoreQuantitativo { get; private set; }
    public decimal ScoreQualitativo { get; private set; }
    public string? Justificativa { get; private set; }
    public string? Indicadores { get; private set; }
    public bool EntrouHoje { get; private set; }
    public bool SaiuHoje { get; private set; }
    public DateOnly DataRanking { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private BatchRanking() { }

    public static BatchRanking Create(
        Guid batchRunId,
        string subEstrategia,
        string ticker,
        string nome,
        int posicao,
        decimal scoreTotal,
        decimal scoreQuant,
        decimal scoreQual,
        string? justificativa,
        string? indicadores,
        bool entrouHoje,
        bool saiuHoje,
        DateOnly dataRanking)
    {
        return new BatchRanking
        {
            Id = Guid.NewGuid(),
            BatchRunId = batchRunId,
            SubEstrategia = subEstrategia,
            Ticker = ticker,
            Nome = nome,
            Posicao = posicao,
            ScoreTotal = scoreTotal,
            ScoreQuantitativo = scoreQuant,
            ScoreQualitativo = scoreQual,
            Justificativa = justificativa,
            Indicadores = indicadores,
            EntrouHoje = entrouHoje,
            SaiuHoje = saiuHoje,
            DataRanking = dataRanking,
            CreatedAt = DateTime.UtcNow
        };
    }
}
