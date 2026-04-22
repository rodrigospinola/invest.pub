using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Invest.Application.Commands.Profile;
using Invest.Application.Commands.Portfolio;
using Invest.Application.Handlers;
using Invest.Application.Queries.Profile;
using Invest.Domain.Entities;
using Invest.Domain.Enums;
using Invest.Domain.Interfaces;
using Invest.Domain.Services;

namespace Invest.Tests.Application;

public class ProfileHandlerTests
{
    private readonly Mock<IUserProfileRepository> _repoMock = new();
    private readonly Mock<IUserSubStrategyRepository> _subStrategyRepoMock = new();
    private readonly Mock<IUserAssetRepository> _assetRepoMock = new();
    private readonly Mock<IValidator<CreateProfileCommand>> _validatorMock = new();
    private readonly AllocationService _allocationService = new();
    private readonly ProfileHandler _sut;

    public ProfileHandlerTests()
    {
        _sut = new ProfileHandler(
            _repoMock.Object,
            _subStrategyRepoMock.Object,
            _assetRepoMock.Object,
            _allocationService,
            _validatorMock.Object);
    }

    // =========================================================
    // Helper
    // =========================================================

    private static UserProfile BuildProfile(
        Guid userId,
        PerfilRisco perfil = PerfilRisco.Conservador,
        decimal valorTotal = 5000m,
        FaixaPatrimonio faixa = FaixaPatrimonio.Ate10k)
    {
        return UserProfile.Create(userId, perfil, valorTotal, faixa, false, null);
    }

    private void SetupValidatorSuccess()
    {
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateProfileCommand>(), default))
            .ReturnsAsync(new ValidationResult());
    }

    private void SetupValidatorFailure(string propertyName, string message)
    {
        var failures = new List<ValidationFailure>
        {
            new(propertyName, message)
        };
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateProfileCommand>(), default))
            .ReturnsAsync(new ValidationResult(failures));
    }

    // =========================================================
    // CreateProfileAsync
    // =========================================================

    [Fact]
    public async Task CreateProfileAsync_SemPerfilExistente_RetornaSuccessComProfileResponse()
    {
        var userId = Guid.NewGuid();
        var command = new CreateProfileCommand(userId, "conservador", 5000m, false, null);

        SetupValidatorSuccess();
        _repoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserProfile?)null);
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<UserProfile>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateProfileAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UserId.Should().Be(userId);
        result.Value.Perfil.Should().Be("conservador");
        result.Value.ValorTotal.Should().Be(5000m);
        result.Value.AlocacaoAlvo.Should().HaveCount(4);
    }

    [Fact]
    public async Task CreateProfileAsync_PerfilJaExiste_RetornaFailureComMensagemJaExiste()
    {
        var userId = Guid.NewGuid();
        var command = new CreateProfileCommand(userId, "moderado", 5000m, false, null);
        var existingProfile = BuildProfile(userId);

        SetupValidatorSuccess();
        _repoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(existingProfile);

        var result = await _sut.CreateProfileAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PROFILE_ALREADY_EXISTS");
        result.ErrorMessage.Should().Contain("já possui");
    }

    [Fact]
    public async Task CreateProfileAsync_ValidacaoFalha_RetornaFailureComErroDeValidacao()
    {
        var userId = Guid.NewGuid();
        var command = new CreateProfileCommand(userId, "", 5000m, false, null);

        SetupValidatorFailure("Perfil", "Perfil é obrigatório.");

        var result = await _sut.CreateProfileAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("VALIDATION_ERROR");
        result.ErrorField.Should().Be("Perfil");
    }

    // =========================================================
    // GetProfileAsync
    // =========================================================

    [Fact]
    public async Task GetProfileAsync_PerfilEncontrado_RetornaSuccessComProfileResponse()
    {
        var userId = Guid.NewGuid();
        var profile = BuildProfile(userId, PerfilRisco.Moderado, 50000m, FaixaPatrimonio.De10kA100k);
        _repoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(profile);

        var query = new GetProfileQuery(userId);
        var result = await _sut.GetProfileAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UserId.Should().Be(userId);
        result.Value.Perfil.Should().Be("moderado");
    }

    [Fact]
    public async Task GetProfileAsync_PerfilNaoEncontrado_RetornaFailureComNaoEncontrado()
    {
        var userId = Guid.NewGuid();
        _repoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserProfile?)null);

        var query = new GetProfileQuery(userId);
        var result = await _sut.GetProfileAsync(query);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PROFILE_NOT_FOUND");
        result.ErrorMessage.Should().Contain("não encontrado");
    }

    // =========================================================
    // GetAllocation
    // =========================================================

    [Fact]
    public void GetAllocation_ConservadorValor5000_Retorna4Classes()
    {
        var query = new GetAllocationQuery("conservador", 5000m);
        var result = _sut.GetAllocation(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Classes.Should().HaveCount(4);
        result.Value.Faixa.Should().Be("ate_10k");
        result.Value.Perfil.Should().Be("conservador");
    }

    [Fact]
    public void GetAllocation_ArrojadoValor150000_Retorna7Classes()
    {
        var query = new GetAllocationQuery("arrojado", 150000m);
        var result = _sut.GetAllocation(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Classes.Should().HaveCount(7);
        result.Value.Faixa.Should().Be("acima_100k");
        result.Value.Perfil.Should().Be("arrojado");
    }

    // =========================================================
    // UpdateProfileAsync
    // =========================================================

    [Fact]
    public async Task UpdateProfileAsync_AtualizacaoSemMudancaDeFaixa_RetornaSuccessComMudouFaixaFalse()
    {
        var userId = Guid.NewGuid();
        // Profile starts at Ate10k with 5000; update to 8000 — still Ate10k
        var profile = BuildProfile(userId, PerfilRisco.Conservador, 5000m, FaixaPatrimonio.Ate10k);
        _repoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(profile);
        _repoMock
            .Setup(r => r.UpdateAsync(It.IsAny<UserProfile>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateProfileCommand(userId, null, 8000m);
        var result = await _sut.UpdateProfileAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.MudouFaixa.Should().BeFalse();
        result.Value.ValorTotal.Should().Be(8000m);
    }

    [Fact]
    public async Task UpdateProfileAsync_ValorSobeParaDe10kA100k_RetornaSuccessComMudouFaixaTrue()
    {
        var userId = Guid.NewGuid();
        // Profile starts at Ate10k with 5000; update to 15000 → upgrades to De10kA100k
        var profile = BuildProfile(userId, PerfilRisco.Conservador, 5000m, FaixaPatrimonio.Ate10k);
        _repoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(profile);
        _repoMock
            .Setup(r => r.UpdateAsync(It.IsAny<UserProfile>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateProfileCommand(userId, null, 15000m);
        var result = await _sut.UpdateProfileAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.MudouFaixa.Should().BeTrue();
        result.Value.Faixa.Should().Be("10k_100k");
    }

    [Fact]
    public async Task UpdateProfileAsync_PerfilNaoEncontrado_RetornaFailure()
    {
        var userId = Guid.NewGuid();
        _repoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserProfile?)null);

        var command = new UpdateProfileCommand(userId, "moderado", 10000m);
        var result = await _sut.UpdateProfileAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PROFILE_NOT_FOUND");
    }

    // =========================================================
    // ResetProfileAsync
    // =========================================================

    [Fact]
    public async Task ResetProfileAsync_SempreRetornaSucesso()
    {
        var userId = Guid.NewGuid();
        SetupDeleteMocksForUser(userId);

        var result = await _sut.ResetProfileAsync(new ResetProfileCommand(userId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ResetProfileAsync_ChamamaDeleteEmOrdemCorreta()
    {
        // Deve apagar ativos → sub-estratégia → perfil, respeitando FKs
        var userId = Guid.NewGuid();
        var callOrder = new List<string>();

        _assetRepoMock
            .Setup(r => r.DeleteAllByUserIdAsync(userId))
            .Callback(() => callOrder.Add("assets"))
            .Returns(Task.CompletedTask);
        _subStrategyRepoMock
            .Setup(r => r.DeleteByUserIdAsync(userId))
            .Callback(() => callOrder.Add("substrategy"))
            .Returns(Task.CompletedTask);
        _repoMock
            .Setup(r => r.DeleteByUserIdAsync(userId))
            .Callback(() => callOrder.Add("profile"))
            .Returns(Task.CompletedTask);

        await _sut.ResetProfileAsync(new ResetProfileCommand(userId));

        callOrder.Should().Equal("assets", "substrategy", "profile");
    }

    [Fact]
    public async Task ResetProfileAsync_ChamaTodosOsTresRepositorios()
    {
        var userId = Guid.NewGuid();
        SetupDeleteMocksForUser(userId);

        await _sut.ResetProfileAsync(new ResetProfileCommand(userId));

        _assetRepoMock.Verify(r => r.DeleteAllByUserIdAsync(userId), Times.Once);
        _subStrategyRepoMock.Verify(r => r.DeleteByUserIdAsync(userId), Times.Once);
        _repoMock.Verify(r => r.DeleteByUserIdAsync(userId), Times.Once);
    }

    private void SetupDeleteMocksForUser(Guid userId)
    {
        _assetRepoMock
            .Setup(r => r.DeleteAllByUserIdAsync(userId))
            .Returns(Task.CompletedTask);
        _subStrategyRepoMock
            .Setup(r => r.DeleteByUserIdAsync(userId))
            .Returns(Task.CompletedTask);
        _repoMock
            .Setup(r => r.DeleteByUserIdAsync(userId))
            .Returns(Task.CompletedTask);
    }

    // =========================================================
    // ComparePortfolioAsync
    // =========================================================

    [Fact]
    public async Task ComparePortfolioAsync_PerfilNaoEncontrado_RetornaFailure()
    {
        var userId = Guid.NewGuid();
        _repoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserProfile?)null);

        var command = new ComparePortfolioCommand(userId, new Dictionary<string, decimal>());
        var result = await _sut.ComparePortfolioAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PROFILE_NOT_FOUND");
    }

    [Fact]
    public async Task ComparePortfolioAsync_CarteiraVazia_RetornaTodasClassesDoAlvoComZeroAtual()
    {
        var userId = Guid.NewGuid();
        // Conservador Ate10k tem 4 classes; carteira vazia → todas com 0% atual
        var profile = BuildProfile(userId, PerfilRisco.Conservador, 5000m, FaixaPatrimonio.Ate10k);
        _repoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(profile);

        var command = new ComparePortfolioCommand(userId, new Dictionary<string, decimal>());
        var result = await _sut.ComparePortfolioAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Comparacao.Should().HaveCount(4);
        result.Value.Comparacao.Should().OnlyContain(c => c.Atual == 0m);
    }

    [Fact]
    public async Task ComparePortfolioAsync_CarteiraSobrepondaEmAcoes_RetornaDesvioPositivo()
    {
        var userId = Guid.NewGuid();
        var profile = BuildProfile(userId, PerfilRisco.Conservador, 5000m, FaixaPatrimonio.Ate10k);
        _repoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(profile);

        // Carteira com 90% em ações (excessiva para conservador)
        var carteira = new Dictionary<string, decimal> { ["RF Pós"] = 10m, ["Ações"] = 90m };
        var command = new ComparePortfolioCommand(userId, carteira);
        var result = await _sut.ComparePortfolioAsync(command);

        result.IsSuccess.Should().BeTrue();
        // A classe "Ações" deve ter desvio positivo (mais do que o alvo)
        var acoes = result.Value!.Comparacao.FirstOrDefault(c => c.Classe == "Ações");
        acoes.Should().NotBeNull();
        acoes!.Delta.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ComparePortfolioAsync_CarteiraComClasseForaDoAlvo_IncluiClasseNaComparacao()
    {
        var userId = Guid.NewGuid();
        // Conservador Ate10k não tem "Internacional" no alvo
        var profile = BuildProfile(userId, PerfilRisco.Conservador, 5000m, FaixaPatrimonio.Ate10k);
        _repoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(profile);

        var carteira = new Dictionary<string, decimal> { ["Internacional"] = 20m };
        var command = new ComparePortfolioCommand(userId, carteira);
        var result = await _sut.ComparePortfolioAsync(command);

        // A classe extra deve aparecer na comparação com alvo = 0
        var internacional = result.Value!.Comparacao.FirstOrDefault(c => c.Classe == "Internacional");
        internacional.Should().NotBeNull();
        internacional!.Recomendado.Should().Be(0m);
    }

    [Fact]
    public async Task ComparePortfolioAsync_PerfilArrojadoAcima100k_Retorna7Classes()
    {
        var userId = Guid.NewGuid();
        var profile = BuildProfile(userId, PerfilRisco.Arrojado, 150000m, FaixaPatrimonio.Acima100k);
        _repoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(profile);

        var command = new ComparePortfolioCommand(userId, new Dictionary<string, decimal>());
        var result = await _sut.ComparePortfolioAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Comparacao.Should().HaveCount(7);
    }
}
