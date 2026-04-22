using FluentAssertions;
using Moq;
using Invest.Application.Handlers;
using Invest.Application.Queries.Ranking;
using Invest.Domain.Entities;
using Invest.Domain.Enums;
using Invest.Domain.Interfaces;

namespace Invest.Tests.Application;

public class RankingHandlerTests
{
    private readonly Mock<IBatchRankingRepository> _rankingRepoMock = new();
    private readonly Mock<IUserSubStrategyRepository> _subStrategyRepoMock = new();
    private readonly RankingHandler _sut;

    public RankingHandlerTests()
    {
        _sut = new RankingHandler(_rankingRepoMock.Object, _subStrategyRepoMock.Object);
    }

    // =========================================================
    // Helpers
    // =========================================================

    private static BatchRanking BuildRanking(
        string sub,
        string ticker,
        int posicao,
        bool entrouHoje = false,
        DateOnly? data = null)
    {
        return BatchRanking.Create(
            Guid.NewGuid(), sub, ticker, ticker.Replace(".SA", ""),
            posicao, 7.5m, 6.0m, 5.0m, "Boa empresa", null,
            entrouHoje, false, data ?? DateOnly.FromDateTime(DateTime.UtcNow));
    }

    private static UserSubStrategy BuildSubStrategy(
        Guid userId,
        SubEstrategiaAcoes acoes = SubEstrategiaAcoes.Valor,
        SubEstrategiaFiis fiis = SubEstrategiaFiis.Renda)
    {
        return UserSubStrategy.Create(userId, acoes, fiis);
    }

    // =========================================================
    // GetTop20Async
    // =========================================================

    [Fact]
    public async Task GetTop20Async_RankingExistente_RetornaSuccessComItens()
    {
        var rankings = Enumerable.Range(1, 10)
            .Select(i => BuildRanking("valor", $"ACAO{i}.SA", i))
            .ToList();

        _rankingRepoMock
            .Setup(r => r.GetLatestBySubEstrategiaAsync("valor", 20))
            .ReturnsAsync(rankings);

        var query = new GetTop20Query("valor");
        var result = await _sut.GetTop20Async(query);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Itens.Should().HaveCount(10);
        result.Value.SubEstrategia.Should().Be("valor");
    }

    [Fact]
    public async Task GetTop20Async_RankingVazio_RetornaFailure()
    {
        _rankingRepoMock
            .Setup(r => r.GetLatestBySubEstrategiaAsync("valor", 20))
            .ReturnsAsync([]);

        var query = new GetTop20Query("valor");
        var result = await _sut.GetTop20Async(query);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("RANKING_NOT_FOUND");
    }

    [Fact]
    public async Task GetTop20Async_ItensMapeadosCorretamente()
    {
        var data = new DateOnly(2025, 6, 15);
        var ranking = new List<BatchRanking>
        {
            BuildRanking("dividendos", "TAEE11.SA", 1, entrouHoje: true, data: data),
        };

        _rankingRepoMock
            .Setup(r => r.GetLatestBySubEstrategiaAsync("dividendos", 20))
            .ReturnsAsync(ranking);

        var result = await _sut.GetTop20Async(new GetTop20Query("dividendos"));

        result.IsSuccess.Should().BeTrue();
        var item = result.Value!.Itens.First();
        item.Posicao.Should().Be(1);
        item.Ticker.Should().Be("TAEE11.SA");
        item.EntrouHoje.Should().BeTrue();
        result.Value.DataRanking.Should().Be(data);
    }

    [Fact]
    public async Task GetTop20Async_MaximoDeVintePosicoes()
    {
        var rankings = Enumerable.Range(1, 20)
            .Select(i => BuildRanking("misto_acoes", $"ACAO{i:D2}.SA", i))
            .ToList();

        _rankingRepoMock
            .Setup(r => r.GetLatestBySubEstrategiaAsync("misto_acoes", 20))
            .ReturnsAsync(rankings);

        // "misto" é normalizado para "misto_acoes" pelo handler
        var result = await _sut.GetTop20Async(new GetTop20Query("misto"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Itens.Should().HaveCount(20);
    }

    // =========================================================
    // GetSuggestionAsync
    // =========================================================

    [Fact]
    public async Task GetSuggestionAsync_SubStrategyNaoEncontrada_RetornaFailure()
    {
        var userId = Guid.NewGuid();
        _subStrategyRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserSubStrategy?)null);

        var result = await _sut.GetSuggestionAsync(new GetSuggestionQuery(userId));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SUB_STRATEGY_NOT_FOUND");
    }

    [Fact]
    public async Task GetSuggestionAsync_SubStrategyExistente_RetornaAcoesFiis()
    {
        var userId = Guid.NewGuid();
        var subStrategy = BuildSubStrategy(userId, SubEstrategiaAcoes.Dividendos, SubEstrategiaFiis.Renda);

        var acoes = Enumerable.Range(1, 15)
            .Select(i => BuildRanking("dividendos", $"ACAO{i:D2}.SA", i))
            .ToList();

        var fiis = Enumerable.Range(1, 8)
            .Select(i => BuildRanking("renda_fiis", $"FII{i:D2}11.SA", i))
            .ToList();

        _subStrategyRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(subStrategy);

        _rankingRepoMock
            .Setup(r => r.GetLatestBySubEstrategiaAsync("dividendos", 20))
            .ReturnsAsync(acoes);

        _rankingRepoMock
            .Setup(r => r.GetLatestBySubEstrategiaAsync("renda_fiis", 20))
            .ReturnsAsync(fiis);

        var result = await _sut.GetSuggestionAsync(new GetSuggestionQuery(userId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.AcoesRec.Should().NotBeEmpty();
        result.Value.FiisRec.Should().NotBeEmpty();
        result.Value.UserId.Should().Be(userId);
    }

    [Theory]
    [InlineData(SubEstrategiaAcoes.Valor,      "valor")]
    [InlineData(SubEstrategiaAcoes.Dividendos,  "dividendos")]
    [InlineData(SubEstrategiaAcoes.Misto,       "misto_acoes")]
    public async Task GetSuggestionAsync_MapeiaSubEstrategiaAcoes(SubEstrategiaAcoes enum_, string expectedStr)
    {
        var userId = Guid.NewGuid();
        var subStrategy = BuildSubStrategy(userId, acoes: enum_, fiis: SubEstrategiaFiis.Renda);

        _subStrategyRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(subStrategy);
        _rankingRepoMock.Setup(r => r.GetLatestBySubEstrategiaAsync(expectedStr, 20)).ReturnsAsync([BuildRanking(expectedStr, "ACAO1.SA", 1)]);
        _rankingRepoMock.Setup(r => r.GetLatestBySubEstrategiaAsync("renda_fiis", 20)).ReturnsAsync([BuildRanking("renda_fiis", "FII1.SA", 1)]);

        var result = await _sut.GetSuggestionAsync(new GetSuggestionQuery(userId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.SubEstrategiaAcoes.Should().Be(expectedStr);
    }

    [Theory]
    [InlineData(SubEstrategiaFiis.Renda,       "renda_fiis")]
    [InlineData(SubEstrategiaFiis.Valorizacao,  "valorizacao_fiis")]
    [InlineData(SubEstrategiaFiis.Misto,        "misto_fiis")]
    public async Task GetSuggestionAsync_MapeiaSubEstrategiaFiis(SubEstrategiaFiis enum_, string expectedStr)
    {
        var userId = Guid.NewGuid();
        var subStrategy = BuildSubStrategy(userId, acoes: SubEstrategiaAcoes.Valor, fiis: enum_);

        _subStrategyRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(subStrategy);
        _rankingRepoMock.Setup(r => r.GetLatestBySubEstrategiaAsync("valor", 20)).ReturnsAsync([BuildRanking("valor", "ACAO1.SA", 1)]);
        _rankingRepoMock.Setup(r => r.GetLatestBySubEstrategiaAsync(expectedStr, 20)).ReturnsAsync([BuildRanking(expectedStr, "FII1.SA", 1)]);

        var result = await _sut.GetSuggestionAsync(new GetSuggestionQuery(userId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.SubEstrategiaFiis.Should().Be(expectedStr);
    }

    [Fact]
    public async Task GetSuggestionAsync_LimitaAcoes15_LimitaFiis8()
    {
        var userId = Guid.NewGuid();
        var subStrategy = BuildSubStrategy(userId);

        // 20 ações e 20 FIIs retornados pelo repo — mas sugestão deve limitar
        var acoes = Enumerable.Range(1, 20).Select(i => BuildRanking("valor", $"ACAO{i:D2}.SA", i)).ToList();
        var fiis  = Enumerable.Range(1, 20).Select(i => BuildRanking("renda_fiis", $"FII{i:D2}11.SA", i)).ToList();

        _subStrategyRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(subStrategy);
        _rankingRepoMock.Setup(r => r.GetLatestBySubEstrategiaAsync("valor", 20)).ReturnsAsync(acoes);
        _rankingRepoMock.Setup(r => r.GetLatestBySubEstrategiaAsync("renda_fiis", 20)).ReturnsAsync(fiis);

        var result = await _sut.GetSuggestionAsync(new GetSuggestionQuery(userId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.AcoesRec.Should().HaveCountLessOrEqualTo(15);
        result.Value.FiisRec.Should().HaveCountLessOrEqualTo(8);
    }
}
