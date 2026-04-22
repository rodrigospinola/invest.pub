using Invest.Application.Common;
using Invest.Application.Queries.Ranking;
using Invest.Application.Responses;
using Invest.Domain.Enums;
using Invest.Domain.Interfaces;

namespace Invest.Application.Handlers;

public class RankingHandler
{
    private readonly IBatchRankingRepository _rankingRepository;
    private readonly IUserSubStrategyRepository _subStrategyRepository;

    public RankingHandler(
        IBatchRankingRepository rankingRepository,
        IUserSubStrategyRepository subStrategyRepository)
    {
        _rankingRepository = rankingRepository;
        _subStrategyRepository = subStrategyRepository;
    }

    public async Task<Result<RankingResponse>> GetTop20Async(GetTop20Query query)
    {
        // Normaliza o nome da sub-estratégia para o formato salvo pelo batch
        // Ex: "renda" → "renda_fiis", "misto" → ambíguo (tenta ações primeiro)
        var subEstrategia = NormalizeSubEstrategia(query.SubEstrategia);

        var rankings = await _rankingRepository.GetLatestBySubEstrategiaAsync(subEstrategia, 20);
        if (rankings.Count == 0)
            return Result<RankingResponse>.Failure("RANKING_NOT_FOUND", "Nenhum ranking encontrado para esta sub-estratégia.");

        var dataRanking = rankings.Max(r => r.DataRanking);
        var itens = rankings.Select(r => new RankingItemResponse(
            r.Posicao,
            r.Ticker,
            r.Nome,
            r.ScoreTotal,
            r.ScoreQuantitativo,
            r.ScoreQualitativo,
            r.Justificativa,
            r.Indicadores,
            r.EntrouHoje,
            r.SaiuHoje
        )).ToList();

        return Result<RankingResponse>.Success(new RankingResponse(subEstrategia, dataRanking, itens));
    }

    public async Task<Result<SuggestionResponse>> GetSuggestionAsync(GetSuggestionQuery query)
    {
        var subStrategy = await _subStrategyRepository.GetByUserIdAsync(query.UserId);
        if (subStrategy == null)
            return Result<SuggestionResponse>.Failure("SUB_STRATEGY_NOT_FOUND",
                "Sub-estratégia não encontrada. Configure suas preferências primeiro.");

        var acoesStr = MapAcoes(subStrategy.SubEstrategiaAcoes);
        var fiisStr = MapFiis(subStrategy.SubEstrategiaFiis);

        var acoesRankings = await _rankingRepository.GetLatestBySubEstrategiaAsync(acoesStr, 20);
        var fiisRankings = await _rankingRepository.GetLatestBySubEstrategiaAsync(fiisStr, 20);

        var acoesRec = acoesRankings
            .OrderBy(r => r.Posicao)
            .Take(15)
            .Select(r => new SuggestionAssetResponse(r.Posicao, r.Ticker, r.Nome, r.ScoreTotal, r.Justificativa))
            .ToList();

        var fiisRec = fiisRankings
            .OrderBy(r => r.Posicao)
            .Take(8)
            .Select(r => new SuggestionAssetResponse(r.Posicao, r.Ticker, r.Nome, r.ScoreTotal, r.Justificativa))
            .ToList();

        return Result<SuggestionResponse>.Success(new SuggestionResponse(
            query.UserId,
            acoesStr,
            fiisStr,
            acoesRec,
            fiisRec
        ));
    }

    /// <summary>
    /// Normaliza o nome da sub-estratégia recebido do frontend para o formato
    /// exato salvo pelo batch no banco de dados.
    /// Batch salva: valor, dividendos, misto_acoes, renda_fiis, valorizacao_fiis, misto_fiis
    /// </summary>
    private static string NormalizeSubEstrategia(string sub) =>
        sub.ToLowerInvariant().Trim() switch
        {
            // Ações
            "valor"          => "valor",
            "dividendos"     => "dividendos",
            "misto_acoes"    => "misto_acoes",
            "misto"          => "misto_acoes",   // ações tem prioridade
            // FIIs
            "renda_fiis"     => "renda_fiis",
            "renda"          => "renda_fiis",
            "valorizacao_fiis" => "valorizacao_fiis",
            "valorizacao"    => "valorizacao_fiis",
            "misto_fiis"     => "misto_fiis",
            // Passthrough para qualquer outro valor
            var other        => other,
        };

    private static string MapAcoes(SubEstrategiaAcoes acoes) => acoes switch
    {
        SubEstrategiaAcoes.Valor      => "valor",
        SubEstrategiaAcoes.Dividendos => "dividendos",
        SubEstrategiaAcoes.Misto      => "misto_acoes",
        _ => throw new ArgumentOutOfRangeException()
    };

    private static string MapFiis(SubEstrategiaFiis fiis) => fiis switch
    {
        SubEstrategiaFiis.Renda       => "renda_fiis",
        SubEstrategiaFiis.Valorizacao => "valorizacao_fiis",
        SubEstrategiaFiis.Misto       => "misto_fiis",
        _ => throw new ArgumentOutOfRangeException()
    };
}
