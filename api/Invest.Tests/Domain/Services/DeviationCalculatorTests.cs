using Invest.Domain.Enums;
using Invest.Domain.Services;
using Xunit;

namespace Invest.Tests.Domain.Services;

public class DeviationCalculatorTests
{
    [Fact]
    public void Calculate_ShouldReturnCorrectDeviations()
    {
        // Arrange
        var calculator = new DeviationCalculator();
        var currentValues = new Dictionary<ClasseAtivo, decimal>
        {
            [ClasseAtivo.Acoes] = 3000m,
            [ClasseAtivo.FundosImobiliarios] = 7000m
        };
        var totalValue = 10000m;
        
        var targetAllocation = new List<(string classe, decimal percentual)>
        {
            ("Ações", 50m),
            ("Fundos imobiliários", 50m)
        };

        // Act
        var result = calculator.Calculate(currentValues, targetAllocation);

        // Assert
        var acoes = result.Find(d => d.Classe == "Ações");
        Assert.NotNull(acoes);
        Assert.Equal(30, acoes.Real);
        Assert.Equal(50, acoes.Alvo);
        Assert.Equal(-20, acoes.Diferenca);

        var fiis = result.Find(d => d.Classe == "Fundos imobiliários");
        Assert.NotNull(fiis);
        Assert.Equal(70, fiis.Real);
        Assert.Equal(50, fiis.Alvo);
        Assert.Equal(20, fiis.Diferenca);
    }
}
