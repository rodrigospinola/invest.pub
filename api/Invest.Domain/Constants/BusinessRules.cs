namespace Invest.Domain.Constants;

public static class BusinessRules
{
    // Buffer de 5% para evitar ping-pong de faixa
    public const decimal FaixaUpgradeAte10k = 10_000m;
    public const decimal FaixaDowngradeAte10k = 9_500m;
    public const decimal FaixaUpgrade10kA100k = 100_000m;
    public const decimal FaixaDowngrade10kA100k = 95_000m;
}
