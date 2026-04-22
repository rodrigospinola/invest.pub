using FluentAssertions;
using Invest.Domain.Services;

namespace Invest.Tests.Domain;

public class AssetQuantityCalculatorTests
{
    // =========================================================
    // QuantidadeAcoes — limites da escala
    // =========================================================

    [Theory]
    [InlineData(0,      5)]
    [InlineData(1000,   5)]
    [InlineData(4999,   5)]
    [InlineData(5000,   5)]  // exatamente no limite inferior
    public void QuantidadeAcoes_ValorAteDe5k_Retorna5(decimal valor, int expected)
    {
        var result = AssetQuantityCalculator.QuantidadeAcoes(valor);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(5001,   7)]
    [InlineData(10000,  7)]
    [InlineData(19999,  7)]
    [InlineData(20000,  7)]  // exatamente no limite
    public void QuantidadeAcoes_ValorEntre5kE20k_Retorna7(decimal valor, int expected)
    {
        var result = AssetQuantityCalculator.QuantidadeAcoes(valor);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(20001,  10)]
    [InlineData(35000,  10)]
    [InlineData(50000,  10)]
    public void QuantidadeAcoes_ValorEntre20kE50k_Retorna10(decimal valor, int expected)
    {
        var result = AssetQuantityCalculator.QuantidadeAcoes(valor);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(50001,  12)]
    [InlineData(75000,  12)]
    [InlineData(100000, 12)]
    public void QuantidadeAcoes_ValorEntre50kE100k_Retorna12(decimal valor, int expected)
    {
        var result = AssetQuantityCalculator.QuantidadeAcoes(valor);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(100001,  15)]
    [InlineData(250000,  15)]
    [InlineData(1000000, 15)]
    public void QuantidadeAcoes_ValorAcima100k_Retorna15(decimal valor, int expected)
    {
        var result = AssetQuantityCalculator.QuantidadeAcoes(valor);
        result.Should().Be(expected);
    }

    [Fact]
    public void QuantidadeAcoes_ValorNegativo_RetornaMinimo()
    {
        var result = AssetQuantityCalculator.QuantidadeAcoes(-1m);
        result.Should().Be(5);
    }

    // =========================================================
    // QuantidadeFiis — limites da escala
    // =========================================================

    [Theory]
    [InlineData(0,    5)]
    [InlineData(2500, 5)]
    [InlineData(5000, 5)]
    public void QuantidadeFiis_ValorAte5k_Retorna5(decimal valor, int expected)
    {
        var result = AssetQuantityCalculator.QuantidadeFiis(valor);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(5001,  6)]
    [InlineData(12000, 6)]
    [InlineData(20000, 6)]
    public void QuantidadeFiis_ValorEntre5kE20k_Retorna6(decimal valor, int expected)
    {
        var result = AssetQuantityCalculator.QuantidadeFiis(valor);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(20001, 7)]
    [InlineData(35000, 7)]
    [InlineData(50000, 7)]
    public void QuantidadeFiis_ValorEntre20kE50k_Retorna7(decimal valor, int expected)
    {
        var result = AssetQuantityCalculator.QuantidadeFiis(valor);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(50001,  8)]
    [InlineData(100000, 8)]
    [InlineData(500000, 8)]
    public void QuantidadeFiis_ValorAcima50k_Retorna8(decimal valor, int expected)
    {
        var result = AssetQuantityCalculator.QuantidadeFiis(valor);
        result.Should().Be(expected);
    }

    // =========================================================
    // Calcular — combinações
    // =========================================================

    [Fact]
    public void Calcular_PatrimonioIniciante_RetornaMinimos()
    {
        // 5k total: 40% ações (R$2k), 20% FIIs (R$1k)
        var (qtdAcoes, qtdFiis) = AssetQuantityCalculator.Calcular(5000m, 40m, 20m);

        qtdAcoes.Should().Be(5);
        qtdFiis.Should().Be(5);
    }

    [Fact]
    public void Calcular_PatrimonioMedio_RetornaQuantidadesEscaladas()
    {
        // 100k total: 30% ações (R$30k > R$20k), 20% FIIs (R$20k)
        var (qtdAcoes, qtdFiis) = AssetQuantityCalculator.Calcular(100_000m, 30m, 20m);

        qtdAcoes.Should().Be(10); // 30k → faixa 20k-50k → 10
        qtdFiis.Should().Be(6);   // 20k → limite exato → 6
    }

    [Fact]
    public void Calcular_PatrimonioAlto_RetornaMaximos()
    {
        // 500k total: 35% ações (R$175k > R$100k), 20% FIIs (R$100k > R$50k)
        var (qtdAcoes, qtdFiis) = AssetQuantityCalculator.Calcular(500_000m, 35m, 20m);

        qtdAcoes.Should().Be(15);
        qtdFiis.Should().Be(8);
    }

    [Fact]
    public void Calcular_PercentuaisZerados_RetornaMinimos()
    {
        var (qtdAcoes, qtdFiis) = AssetQuantityCalculator.Calcular(200_000m, 0m, 0m);

        qtdAcoes.Should().Be(5);
        qtdFiis.Should().Be(5);
    }

    // =========================================================
    // Garantias dos limites absolutos
    // =========================================================

    [Fact]
    public void QuantidadeAcoes_NuncaMenorQue5()
    {
        AssetQuantityCalculator.QuantidadeAcoes(0m).Should().BeGreaterOrEqualTo(5);
    }

    [Fact]
    public void QuantidadeAcoes_NuncaMaiorQue15()
    {
        AssetQuantityCalculator.QuantidadeAcoes(decimal.MaxValue).Should().BeLessOrEqualTo(15);
    }

    [Fact]
    public void QuantidadeFiis_NuncaMenorQue5()
    {
        AssetQuantityCalculator.QuantidadeFiis(0m).Should().BeGreaterOrEqualTo(5);
    }

    [Fact]
    public void QuantidadeFiis_NuncaMaiorQue8()
    {
        AssetQuantityCalculator.QuantidadeFiis(decimal.MaxValue).Should().BeLessOrEqualTo(8);
    }
}
