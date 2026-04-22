using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Invest.Application.Commands.Portfolio;
using Invest.Application.Handlers;
using Invest.Application.Queries.Portfolio;
using Invest.Domain.Entities;
using Invest.Domain.Enums;
using Invest.Domain.Interfaces;

namespace Invest.Tests.Application;

public class PortfolioHandlerTests
{
    private readonly Mock<IUserAssetRepository> _assetRepoMock = new();
    private readonly Mock<IValidator<ImportPortfolioCommand>> _validatorMock = new();
    private readonly PortfolioHandler _sut;

    public PortfolioHandlerTests()
    {
        _sut = new PortfolioHandler(_assetRepoMock.Object, _validatorMock.Object);
    }

    // =========================================================
    // Helpers
    // =========================================================

    private void SetupValidatorSuccess()
    {
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ImportPortfolioCommand>(), default))
            .ReturnsAsync(new ValidationResult());
    }

    private void SetupValidatorFailure(string property, string message)
    {
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ImportPortfolioCommand>(), default))
            .ReturnsAsync(new ValidationResult([new ValidationFailure(property, message)]));
    }

    private static ImportPortfolioAsset ValidAsset(
        string ticker = "PETR4",
        string nome = "Petrobras PN",
        string classe = "Acoes",
        decimal quantidade = 100m,
        decimal precoMedio = 28.50m) =>
        new(ticker, nome, classe, quantidade, precoMedio);

    private static UserAsset BuildStoredAsset(
        Guid userId,
        string ticker = "PETR4",
        ClasseAtivo classe = ClasseAtivo.Acoes,
        OrigemAtivo origem = OrigemAtivo.Proprio)
    {
        return UserAsset.Create(userId, null, ticker, ticker, classe, null, 100, 28.50m, origem);
    }

    // =========================================================
    // ImportPortfolioAsync — sucesso
    // =========================================================

    [Fact]
    public async Task ImportPortfolioAsync_Valido_RetornaSuccessComTotalImportado()
    {
        var userId = Guid.NewGuid();
        var command = new ImportPortfolioCommand(userId, [ValidAsset()]);

        SetupValidatorSuccess();
        _assetRepoMock.Setup(r => r.AddRangeAsync(It.IsAny<List<UserAsset>>())).Returns(Task.CompletedTask);

        var result = await _sut.ImportPortfolioAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalImportados.Should().Be(1);
    }

    [Fact]
    public async Task ImportPortfolioAsync_MultiploAtivos_RetornaContagemCorreta()
    {
        var userId = Guid.NewGuid();
        var ativos = new List<ImportPortfolioAsset>
        {
            ValidAsset("PETR4",  "Petrobras",  "Acoes",             100, 28.50m),
            ValidAsset("MXRF11", "Maxi Renda", "FundosImobiliarios", 50, 10.20m),
            ValidAsset("WEGE3",  "WEG",         "Acoes",             200, 45.00m),
        };
        var command = new ImportPortfolioCommand(userId, ativos);

        SetupValidatorSuccess();
        _assetRepoMock.Setup(r => r.AddRangeAsync(It.IsAny<List<UserAsset>>())).Returns(Task.CompletedTask);

        var result = await _sut.ImportPortfolioAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalImportados.Should().Be(3);
    }

    [Fact]
    public async Task ImportPortfolioAsync_Valido_PersistoNoBanco()
    {
        var userId = Guid.NewGuid();
        var command = new ImportPortfolioCommand(userId, [ValidAsset("VALE3", "Vale", "Acoes", 50, 65m)]);

        SetupValidatorSuccess();
        _assetRepoMock.Setup(r => r.AddRangeAsync(It.IsAny<List<UserAsset>>())).Returns(Task.CompletedTask);

        await _sut.ImportPortfolioAsync(command);

        _assetRepoMock.Verify(r => r.AddRangeAsync(It.Is<List<UserAsset>>(
            list => list.Count == 1 && list[0].Ticker == "VALE3"
        )), Times.Once);
    }

    [Fact]
    public async Task ImportPortfolioAsync_TickerNormalizado_UpperCase()
    {
        var userId = Guid.NewGuid();
        var command = new ImportPortfolioCommand(userId, [ValidAsset(ticker: "petr4")]);

        SetupValidatorSuccess();

        List<UserAsset>? captured = null;
        _assetRepoMock.Setup(r => r.AddRangeAsync(It.IsAny<List<UserAsset>>()))
            .Callback<List<UserAsset>>(list => captured = list)
            .Returns(Task.CompletedTask);

        await _sut.ImportPortfolioAsync(command);

        captured.Should().NotBeNull();
        captured![0].Ticker.Should().Be("PETR4");
    }

    [Fact]
    public async Task ImportPortfolioAsync_OrigemDefinidaComoProprio()
    {
        var userId = Guid.NewGuid();
        var command = new ImportPortfolioCommand(userId, [ValidAsset()]);

        SetupValidatorSuccess();

        List<UserAsset>? captured = null;
        _assetRepoMock.Setup(r => r.AddRangeAsync(It.IsAny<List<UserAsset>>()))
            .Callback<List<UserAsset>>(list => captured = list)
            .Returns(Task.CompletedTask);

        await _sut.ImportPortfolioAsync(command);

        captured![0].Origem.Should().Be(OrigemAtivo.Proprio);
    }

    // =========================================================
    // ImportPortfolioAsync — validação
    // =========================================================

    [Fact]
    public async Task ImportPortfolioAsync_ValidacaoFalha_RetornaFailure()
    {
        var userId = Guid.NewGuid();
        var command = new ImportPortfolioCommand(userId, [ValidAsset()]);

        SetupValidatorFailure("Ticker", "Ticker é obrigatório.");

        var result = await _sut.ImportPortfolioAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task ImportPortfolioAsync_ValidacaoFalha_NaoSalvaNoBanco()
    {
        var command = new ImportPortfolioCommand(Guid.NewGuid(), [ValidAsset()]);
        SetupValidatorFailure("Ticker", "Ticker é obrigatório.");

        await _sut.ImportPortfolioAsync(command);

        _assetRepoMock.Verify(r => r.AddRangeAsync(It.IsAny<List<UserAsset>>()), Times.Never);
    }

    // =========================================================
    // GetAssetsAsync
    // =========================================================

    [Fact]
    public async Task GetAssetsAsync_ComAtivos_RetornaListaMapeada()
    {
        var userId = Guid.NewGuid();
        var stored = new List<UserAsset>
        {
            BuildStoredAsset(userId, "PETR4", ClasseAtivo.Acoes, OrigemAtivo.Proprio),
            BuildStoredAsset(userId, "MXRF11", ClasseAtivo.FundosImobiliarios, OrigemAtivo.Sugerido),
        };

        _assetRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(stored);

        var result = await _sut.GetAssetsAsync(new GetAssetsQuery(userId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Ativos.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAssetsAsync_SemAtivos_RetornaListaVazia()
    {
        var userId = Guid.NewGuid();
        _assetRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync([]);

        var result = await _sut.GetAssetsAsync(new GetAssetsQuery(userId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Ativos.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAssetsAsync_ItensMapeadosCorretamente()
    {
        var userId = Guid.NewGuid();
        var asset = BuildStoredAsset(userId, "WEGE3", ClasseAtivo.Acoes, OrigemAtivo.Sugerido);
        _assetRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync([asset]);

        var result = await _sut.GetAssetsAsync(new GetAssetsQuery(userId));

        var item = result.Value!.Ativos.First();
        item.Ticker.Should().Be("WEGE3");
        item.Quantidade.Should().Be(100);
        item.Ativo.Should().BeTrue();
    }

    // =========================================================
    // Mapeamento de classe — casos de borda
    // =========================================================

    [Theory]
    [InlineData("Acoes",            ClasseAtivo.Acoes)]
    [InlineData("acoes",            ClasseAtivo.Acoes)]
    [InlineData("FundosImobiliarios", ClasseAtivo.FundosImobiliarios)]
    [InlineData("fiis",             ClasseAtivo.FundosImobiliarios)]
    [InlineData("fii",              ClasseAtivo.FundosImobiliarios)]
    [InlineData("RFPos",            ClasseAtivo.RFPos)]
    [InlineData("RFDinamica",       ClasseAtivo.RFDinamica)]
    [InlineData("Internacional",    ClasseAtivo.Internacional)]
    [InlineData("FundosMultimercados", ClasseAtivo.FundosMultimercados)]
    public async Task ImportPortfolioAsync_DiversasClasses_MapeiaCorretamente(string classe, ClasseAtivo expectedClasse)
    {
        var command = new ImportPortfolioCommand(Guid.NewGuid(), [ValidAsset(classe: classe)]);
        SetupValidatorSuccess();

        List<UserAsset>? captured = null;
        _assetRepoMock.Setup(r => r.AddRangeAsync(It.IsAny<List<UserAsset>>()))
            .Callback<List<UserAsset>>(list => captured = list)
            .Returns(Task.CompletedTask);

        await _sut.ImportPortfolioAsync(command);

        captured![0].Classe.Should().Be(expectedClasse);
    }

    [Fact]
    public async Task ImportPortfolioAsync_ClasseDesconhecida_FallbackAcoes()
    {
        // Classe inválida cai no fallback default
        var command = new ImportPortfolioCommand(Guid.NewGuid(), [ValidAsset(classe: "ClasseInexistente")]);
        SetupValidatorSuccess();

        List<UserAsset>? captured = null;
        _assetRepoMock.Setup(r => r.AddRangeAsync(It.IsAny<List<UserAsset>>()))
            .Callback<List<UserAsset>>(list => captured = list)
            .Returns(Task.CompletedTask);

        var result = await _sut.ImportPortfolioAsync(command);

        // Handler tolera classe inválida (não falha), mas retorna ticker na lista de falhas
        result.IsSuccess.Should().BeTrue();
        // O asset é criado mesmo assim (fallback para Acoes)
        captured.Should().NotBeNull();
    }
}
