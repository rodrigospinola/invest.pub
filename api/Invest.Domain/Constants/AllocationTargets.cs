using Invest.Domain.Enums;

namespace Invest.Domain.Constants;

public static class AllocationTargets
{
    // [faixa][perfil] = lista de (classe, percentual)
    private static readonly Dictionary<FaixaPatrimonio, Dictionary<PerfilRisco, List<(string classe, decimal percentual)>>> _targets = new()
    {
        [FaixaPatrimonio.Ate10k] = new()
        {
            [PerfilRisco.Conservador] = new()
            {
                ("RF Dinâmica", 45m), ("RF Pós", 30m), ("Fundos imobiliários", 15m), ("Ações", 10m)
            },
            [PerfilRisco.Moderado] = new()
            {
                ("RF Dinâmica", 30m), ("RF Pós", 25m), ("Fundos imobiliários", 22m), ("Ações", 23m)
            },
            [PerfilRisco.Arrojado] = new()
            {
                ("RF Dinâmica", 22m), ("RF Pós", 18m), ("Fundos imobiliários", 25m), ("Ações", 35m)
            }
        },
        [FaixaPatrimonio.De10kA100k] = new()
        {
            [PerfilRisco.Conservador] = new()
            {
                ("RF Dinâmica", 35m), ("RF Pós", 28m), ("Fundos imobiliários", 15m), ("Ações", 12m), ("Internacional", 10m)
            },
            [PerfilRisco.Moderado] = new()
            {
                ("RF Dinâmica", 24m), ("RF Pós", 22m), ("Fundos imobiliários", 18m), ("Ações", 18m), ("Internacional", 18m)
            },
            [PerfilRisco.Arrojado] = new()
            {
                ("RF Dinâmica", 22m), ("RF Pós", 17m), ("Fundos imobiliários", 16m), ("Ações", 20m), ("Internacional", 25m)
            }
        },
        [FaixaPatrimonio.Acima100k] = new()
        {
            [PerfilRisco.Conservador] = new()
            {
                ("RF Dinâmica", 25m), ("RF Pós", 25m), ("Fundos imobiliários", 15m), ("Ações", 10m),
                ("Internacional", 10m), ("Fundos multimercados", 14m), ("Alternativos", 1m)
            },
            [PerfilRisco.Moderado] = new()
            {
                ("RF Dinâmica", 20m), ("RF Pós", 20m), ("Fundos imobiliários", 15m), ("Ações", 15m),
                ("Internacional", 15m), ("Fundos multimercados", 14m), ("Alternativos", 1m)
            },
            [PerfilRisco.Arrojado] = new()
            {
                ("RF Dinâmica", 20m), ("RF Pós", 15m), ("Fundos imobiliários", 15m), ("Ações", 15m),
                ("Internacional", 20m), ("Fundos multimercados", 12m), ("Alternativos", 3m)
            }
        }
    };

    public static List<(string classe, decimal percentual)> Get(FaixaPatrimonio faixa, PerfilRisco perfil)
        => _targets[faixa][perfil];
}
