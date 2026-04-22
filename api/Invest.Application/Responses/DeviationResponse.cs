namespace Invest.Application.Responses;

public record DeviationResponse(
    string Classe,
    decimal Real,
    decimal Alvo,
    decimal Diferenca,
    decimal DiferencaPercentual,
    bool IsAlertaExtraordinario
);
