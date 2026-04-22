namespace Invest.Domain.Services;

/// <summary>
/// Calcula a quantidade sugerida de ativos (ações e FIIs) de acordo com o valor alocado.
///
/// Regras de negócio:
/// - Ações: 5 a 15 ativos, escalando com o valor alocado
/// - FIIs:  5 a 8 ativos, escalando com o valor alocado
///
/// Escala progressiva:
/// Ações:
///   Até R$5k    → 5 ativos
///   R$5k–R$20k  → 7 ativos
///   R$20k–R$50k → 10 ativos
///   R$50k–R$100k→ 12 ativos
///   Acima R$100k→ 15 ativos
///
/// FIIs:
///   Até R$5k    → 5 ativos
///   R$5k–R$20k  → 6 ativos
///   R$20k–R$50k → 7 ativos
///   Acima R$50k → 8 ativos
/// </summary>
public static class AssetQuantityCalculator
{
    // Ações — limites (valor alocado, não patrimônio total)
    private static readonly (decimal Limite, int Quantidade)[] _acoesEscala =
    [
        (5_000m,   5),
        (20_000m,  7),
        (50_000m,  10),
        (100_000m, 12),
        (decimal.MaxValue, 15),
    ];

    // FIIs — limites
    private static readonly (decimal Limite, int Quantidade)[] _fiisEscala =
    [
        (5_000m,  5),
        (20_000m, 6),
        (50_000m, 7),
        (decimal.MaxValue, 8),
    ];

    /// <summary>
    /// Quantidade de ações sugeridas para um dado valor alocado em ações.
    /// </summary>
    /// <param name="valorAlocadoAcoes">Valor em reais destinado a ações.</param>
    /// <returns>Número de ações (entre 5 e 15).</returns>
    public static int QuantidadeAcoes(decimal valorAlocadoAcoes)
        => ResolveQuantidade(_acoesEscala, valorAlocadoAcoes);

    /// <summary>
    /// Quantidade de FIIs sugeridos para um dado valor alocado em FIIs.
    /// </summary>
    /// <param name="valorAlocadoFiis">Valor em reais destinado a FIIs.</param>
    /// <returns>Número de FIIs (entre 5 e 8).</returns>
    public static int QuantidadeFiis(decimal valorAlocadoFiis)
        => ResolveQuantidade(_fiisEscala, valorAlocadoFiis);

    /// <summary>
    /// Calcula as quantidades de ações e FIIs a partir do patrimônio total e da alocação-alvo.
    /// </summary>
    /// <param name="patrimonioTotal">Patrimônio total do investidor em reais.</param>
    /// <param name="percentualAcoes">Percentual (0–100) destinado a ações.</param>
    /// <param name="percentualFiis">Percentual (0–100) destinado a FIIs.</param>
    /// <returns>Tupla (qtdAcoes, qtdFiis).</returns>
    public static (int QtdAcoes, int QtdFiis) Calcular(
        decimal patrimonioTotal,
        decimal percentualAcoes,
        decimal percentualFiis)
    {
        var valorAcoes = patrimonioTotal * (percentualAcoes / 100m);
        var valorFiis  = patrimonioTotal * (percentualFiis / 100m);

        return (QuantidadeAcoes(valorAcoes), QuantidadeFiis(valorFiis));
    }

    private static int ResolveQuantidade((decimal Limite, int Quantidade)[] escala, decimal valor)
    {
        if (valor < 0) return escala[0].Quantidade;

        foreach (var (limite, quantidade) in escala)
        {
            if (valor <= limite)
                return quantidade;
        }

        return escala[^1].Quantidade;
    }
}
