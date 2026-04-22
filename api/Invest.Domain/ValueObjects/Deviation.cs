namespace Invest.Domain.ValueObjects;

public record Deviation(string Classe, decimal Real, decimal Alvo)
{
    public decimal Diferenca => Real - Alvo;
    public decimal DiferencaPercentual => Alvo > 0 ? (Real - Alvo) / Alvo * 100 : 0;
    public bool IsAlertaExtraordinario => Math.Abs(Diferenca) > 5; // > 5% de desvio absoluto
}
