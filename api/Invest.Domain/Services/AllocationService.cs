using Invest.Domain.Constants;
using Invest.Domain.Enums;

namespace Invest.Domain.Services;

public class AllocationService
{
    public FaixaPatrimonio DeterminarFaixa(decimal valorTotal)
    {
        if (valorTotal < BusinessRules.FaixaUpgradeAte10k)
            return FaixaPatrimonio.Ate10k;
        if (valorTotal < BusinessRules.FaixaUpgrade10kA100k)
            return FaixaPatrimonio.De10kA100k;
        return FaixaPatrimonio.Acima100k;
    }

    public List<(string classe, decimal percentual)> ObterAlocacao(PerfilRisco perfil, decimal valorTotal)
    {
        var faixa = DeterminarFaixa(valorTotal);
        return AllocationTargets.Get(faixa, perfil);
    }

    public FaixaPatrimonio DeterminarFaixaComBuffer(decimal valorAtual, FaixaPatrimonio faixaAtual)
    {
        // Aplica buffer de 5% para evitar ping-pong
        return faixaAtual switch
        {
            FaixaPatrimonio.Ate10k when valorAtual >= BusinessRules.FaixaUpgradeAte10k
                => FaixaPatrimonio.De10kA100k,
            FaixaPatrimonio.De10kA100k when valorAtual < BusinessRules.FaixaDowngradeAte10k
                => FaixaPatrimonio.Ate10k,
            FaixaPatrimonio.De10kA100k when valorAtual >= BusinessRules.FaixaUpgrade10kA100k
                => FaixaPatrimonio.Acima100k,
            FaixaPatrimonio.Acima100k when valorAtual < BusinessRules.FaixaDowngrade10kA100k
                => FaixaPatrimonio.De10kA100k,
            _ => faixaAtual
        };
    }
}
