using FluentAssertions;
using Invest.Domain.Constants;
using Invest.Domain.Enums;
using Invest.Domain.Services;

namespace Invest.Tests.Domain;

public class AllocationServiceTests
{
    private readonly AllocationService _sut = new();

    // =========================================================
    // DeterminarFaixa
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(5000)]
    [InlineData(9999.99)]
    public void DeterminarFaixa_ValorAbaixoDe10k_RetornaAte10k(decimal valor)
    {
        var result = _sut.DeterminarFaixa(valor);
        result.Should().Be(FaixaPatrimonio.Ate10k);
    }

    [Theory]
    [InlineData(10000)]
    [InlineData(50000)]
    [InlineData(99999.99)]
    public void DeterminarFaixa_ValorEntre10kE100k_RetornaDe10kA100k(decimal valor)
    {
        var result = _sut.DeterminarFaixa(valor);
        result.Should().Be(FaixaPatrimonio.De10kA100k);
    }

    [Theory]
    [InlineData(100000)]
    [InlineData(500000)]
    public void DeterminarFaixa_ValorAcimaDe100k_RetornaAcima100k(decimal valor)
    {
        var result = _sut.DeterminarFaixa(valor);
        result.Should().Be(FaixaPatrimonio.Acima100k);
    }

    // =========================================================
    // ObterAlocacao — count of classes per faixa
    // =========================================================

    [Theory]
    [InlineData(PerfilRisco.Conservador, 5000, 4)]
    [InlineData(PerfilRisco.Moderado,    5000, 4)]
    [InlineData(PerfilRisco.Arrojado,    5000, 4)]
    public void ObterAlocacao_FaixaAte10k_Retorna4Classes(PerfilRisco perfil, decimal valor, int expectedCount)
    {
        var result = _sut.ObterAlocacao(perfil, valor);
        result.Should().HaveCount(expectedCount);
    }

    [Theory]
    [InlineData(PerfilRisco.Conservador, 50000, 5)]
    [InlineData(PerfilRisco.Moderado,    50000, 5)]
    [InlineData(PerfilRisco.Arrojado,    50000, 5)]
    public void ObterAlocacao_FaixaDe10kA100k_Retorna5Classes(PerfilRisco perfil, decimal valor, int expectedCount)
    {
        var result = _sut.ObterAlocacao(perfil, valor);
        result.Should().HaveCount(expectedCount);
    }

    [Theory]
    [InlineData(PerfilRisco.Conservador, 150000, 7)]
    [InlineData(PerfilRisco.Moderado,    150000, 7)]
    [InlineData(PerfilRisco.Arrojado,    150000, 7)]
    public void ObterAlocacao_FaixaAcima100k_Retorna7Classes(PerfilRisco perfil, decimal valor, int expectedCount)
    {
        var result = _sut.ObterAlocacao(perfil, valor);
        result.Should().HaveCount(expectedCount);
    }

    // =========================================================
    // ObterAlocacao — percentages sum to 100
    // =========================================================

    [Theory]
    [InlineData(PerfilRisco.Conservador, 5000)]
    [InlineData(PerfilRisco.Moderado,    5000)]
    [InlineData(PerfilRisco.Arrojado,    5000)]
    [InlineData(PerfilRisco.Conservador, 50000)]
    [InlineData(PerfilRisco.Moderado,    50000)]
    [InlineData(PerfilRisco.Arrojado,    50000)]
    [InlineData(PerfilRisco.Conservador, 150000)]
    [InlineData(PerfilRisco.Moderado,    150000)]
    [InlineData(PerfilRisco.Arrojado,    150000)]
    public void ObterAlocacao_TodosCombinacoes_PercentuaisSomam100(PerfilRisco perfil, decimal valor)
    {
        var result = _sut.ObterAlocacao(perfil, valor);
        var soma = result.Sum(a => a.percentual);
        soma.Should().Be(100m);
    }

    // =========================================================
    // DeterminarFaixaComBuffer — upgrade paths
    // =========================================================

    [Fact]
    public void DeterminarFaixaComBuffer_Ate10k_ValorExato10k_UpgradesParaDe10kA100k()
    {
        var result = _sut.DeterminarFaixaComBuffer(10_000m, FaixaPatrimonio.Ate10k);
        result.Should().Be(FaixaPatrimonio.De10kA100k);
    }

    [Fact]
    public void DeterminarFaixaComBuffer_Ate10k_ValorAbaixoDe10k_PermanecerAte10k()
    {
        // 9800 < 10000 so upgrade threshold not met, stays Ate10k
        var result = _sut.DeterminarFaixaComBuffer(9_800m, FaixaPatrimonio.Ate10k);
        result.Should().Be(FaixaPatrimonio.Ate10k);
    }

    [Fact]
    public void DeterminarFaixaComBuffer_De10kA100k_ValorAbaixoDowngrade_DowngradesParaAte10k()
    {
        // 9500 is exactly the downgrade threshold (< 9500 triggers downgrade)
        // BusinessRules.FaixaDowngradeAte10k = 9500, condition is < 9500 so use 9499.99
        var result = _sut.DeterminarFaixaComBuffer(9_499.99m, FaixaPatrimonio.De10kA100k);
        result.Should().Be(FaixaPatrimonio.Ate10k);
    }

    [Fact]
    public void DeterminarFaixaComBuffer_De10kA100k_Valor9500_PermanecerDe10kA100k()
    {
        // 9500 is NOT < 9500, so no downgrade
        var result = _sut.DeterminarFaixaComBuffer(9_500m, FaixaPatrimonio.De10kA100k);
        result.Should().Be(FaixaPatrimonio.De10kA100k);
    }

    [Fact]
    public void DeterminarFaixaComBuffer_De10kA100k_ValorExato100k_UpgradesParaAcima100k()
    {
        var result = _sut.DeterminarFaixaComBuffer(100_000m, FaixaPatrimonio.De10kA100k);
        result.Should().Be(FaixaPatrimonio.Acima100k);
    }

    [Fact]
    public void DeterminarFaixaComBuffer_Acima100k_ValorAbaixoDowngrade_DowngradesParaDe10kA100k()
    {
        // 95000 is exactly BusinessRules.FaixaDowngrade10kA100k, condition < 95000 => use 94999.99
        var result = _sut.DeterminarFaixaComBuffer(94_999.99m, FaixaPatrimonio.Acima100k);
        result.Should().Be(FaixaPatrimonio.De10kA100k);
    }

    [Fact]
    public void DeterminarFaixaComBuffer_Acima100k_Valor95000_PermanecerAcima100k()
    {
        // 95000 is NOT < 95000, so no downgrade
        var result = _sut.DeterminarFaixaComBuffer(95_000m, FaixaPatrimonio.Acima100k);
        result.Should().Be(FaixaPatrimonio.Acima100k);
    }

    [Fact]
    public void DeterminarFaixaComBuffer_Acima100k_Valor96000_PermanecerAcima100k()
    {
        var result = _sut.DeterminarFaixaComBuffer(96_000m, FaixaPatrimonio.Acima100k);
        result.Should().Be(FaixaPatrimonio.Acima100k);
    }
}
