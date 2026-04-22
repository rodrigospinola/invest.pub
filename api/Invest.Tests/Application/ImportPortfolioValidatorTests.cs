using FluentAssertions;
using Invest.Application.Commands.Portfolio;
using Invest.Application.Validators;

namespace Invest.Tests.Application;

public class ImportPortfolioValidatorTests
{
    private readonly ImportPortfolioValidator _sut = new();

    // =========================================================
    // Helpers
    // =========================================================

    private static ImportPortfolioAsset ValidAsset(
        string ticker = "PETR4",
        string nome = "Petrobras PN",
        string classe = "Acoes",
        decimal quantidade = 100m,
        decimal precoMedio = 28.50m) =>
        new(ticker, nome, classe, quantidade, precoMedio);

    // =========================================================
    // Lista vazia
    // =========================================================

    [Fact]
    public async Task Validate_ListaVazia_RetornaInvalido()
    {
        var command = new ImportPortfolioCommand(Guid.NewGuid(), []);

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("não pode estar vazia"));
    }

    // =========================================================
    // Ativo válido
    // =========================================================

    [Fact]
    public async Task Validate_AtivoValido_RetornaValido()
    {
        var command = new ImportPortfolioCommand(Guid.NewGuid(), [ValidAsset()]);

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_MultiploAtivoValidos_RetornaValido()
    {
        var ativos = new List<ImportPortfolioAsset>
        {
            ValidAsset("PETR4", "Petrobras", "Acoes",              100, 28.50m),
            ValidAsset("MXRF11", "Maxi Renda", "FundosImobiliarios", 50, 10.20m),
            ValidAsset("WEGE3",  "WEG",         "Acoes",             200, 45.00m),
        };
        var command = new ImportPortfolioCommand(Guid.NewGuid(), ativos);

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // =========================================================
    // Ticker obrigatório
    // =========================================================

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_TickerVazio_RetornaInvalido(string ticker)
    {
        var command = new ImportPortfolioCommand(Guid.NewGuid(), [ValidAsset(ticker: ticker)]);

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Ticker"));
    }

    // =========================================================
    // Nome obrigatório
    // =========================================================

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_NomeVazio_RetornaInvalido(string nome)
    {
        var command = new ImportPortfolioCommand(Guid.NewGuid(), [ValidAsset(nome: nome)]);

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Nome"));
    }

    // =========================================================
    // Quantidade inválida
    // =========================================================

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(-100d)]
    public async Task Validate_QuantidadeZeroOuNegativa_RetornaInvalido(decimal quantidade)
    {
        var command = new ImportPortfolioCommand(Guid.NewGuid(), [ValidAsset(quantidade: quantidade)]);

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Quantidade"));
    }

    // =========================================================
    // Preço médio inválido
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public async Task Validate_PrecoMedioZeroOuNegativo_RetornaInvalido(decimal preco)
    {
        var command = new ImportPortfolioCommand(Guid.NewGuid(), [ValidAsset(precoMedio: preco)]);

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Preço médio"));
    }

    // =========================================================
    // Múltiplos erros
    // =========================================================

    [Fact]
    public async Task Validate_AtivoComVariosErros_RetornaTodosOsErros()
    {
        var ativoInvalido = new ImportPortfolioAsset("", "", "Acoes", 0, 0m);
        var command = new ImportPortfolioCommand(Guid.NewGuid(), [ativoInvalido]);

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(4); // ticker, nome, quantidade, preco
    }

    // =========================================================
    // Mistura de válidos e inválidos
    // =========================================================

    [Fact]
    public async Task Validate_UmAtivoValidoUmInvalido_RetornaInvalido()
    {
        var ativos = new List<ImportPortfolioAsset>
        {
            ValidAsset(),
            new("", "Sem Ticker", "Acoes", 10, 5m),
        };
        var command = new ImportPortfolioCommand(Guid.NewGuid(), ativos);

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }
}
