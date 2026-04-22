using Invest.Application.Commands.SubStrategy;
using Invest.Application.Common;
using Invest.Application.Queries.SubStrategy;
using Invest.Application.Responses;
using Invest.Domain.Entities;
using Invest.Domain.Enums;
using Invest.Domain.Interfaces;

namespace Invest.Application.Handlers;

public class SubStrategyHandler
{
    private readonly IUserSubStrategyRepository _subStrategyRepository;

    public SubStrategyHandler(IUserSubStrategyRepository subStrategyRepository)
    {
        _subStrategyRepository = subStrategyRepository;
    }

    public async Task<Result<SubStrategyResponse>> CreateSubStrategyAsync(CreateSubStrategyCommand command)
    {
        if (!TryParseAcoes(command.SubEstrategiaAcoes, out var acoes))
            return Result<SubStrategyResponse>.Failure(
                "VALIDATION_ERROR",
                "Sub-estratégia de ações inválida. Valores aceitos: valor, dividendos, misto.",
                nameof(command.SubEstrategiaAcoes));

        if (!TryParseFiis(command.SubEstrategiaFiis, out var fiis))
            return Result<SubStrategyResponse>.Failure(
                "VALIDATION_ERROR",
                "Sub-estratégia de FIIs inválida. Valores aceitos: renda, valorizacao, misto.",
                nameof(command.SubEstrategiaFiis));

        var existing = await _subStrategyRepository.GetByUserIdAsync(command.UserId);
        if (existing != null)
        {
            existing.Update(acoes, fiis);
            await _subStrategyRepository.UpdateAsync(existing);
            return Result<SubStrategyResponse>.Success(MapToResponse(existing));
        }

        var subStrategy = UserSubStrategy.Create(command.UserId, acoes, fiis);
        await _subStrategyRepository.AddAsync(subStrategy);
        return Result<SubStrategyResponse>.Success(MapToResponse(subStrategy));
    }

    public async Task<Result<SubStrategyResponse>> GetSubStrategyAsync(GetSubStrategyQuery query)
    {
        var subStrategy = await _subStrategyRepository.GetByUserIdAsync(query.UserId);
        if (subStrategy == null)
            return Result<SubStrategyResponse>.Failure("SUB_STRATEGY_NOT_FOUND", "Sub-estratégia não encontrada.");

        return Result<SubStrategyResponse>.Success(MapToResponse(subStrategy));
    }

    private static SubStrategyResponse MapToResponse(UserSubStrategy s) =>
        new(s.UserId, MapAcoes(s.SubEstrategiaAcoes), MapFiis(s.SubEstrategiaFiis), s.CreatedAt);

    private static bool TryParseAcoes(string value, out SubEstrategiaAcoes result)
    {
        switch (value.ToLower())
        {
            case "valor": result = SubEstrategiaAcoes.Valor; return true;
            case "dividendos": result = SubEstrategiaAcoes.Dividendos; return true;
            case "misto": result = SubEstrategiaAcoes.Misto; return true;
            default: result = default; return false;
        }
    }

    private static bool TryParseFiis(string value, out SubEstrategiaFiis result)
    {
        switch (value.ToLower())
        {
            case "renda": result = SubEstrategiaFiis.Renda; return true;
            case "valorizacao": result = SubEstrategiaFiis.Valorizacao; return true;
            case "misto": result = SubEstrategiaFiis.Misto; return true;
            default: result = default; return false;
        }
    }

    private static string MapAcoes(SubEstrategiaAcoes acoes) => acoes switch
    {
        SubEstrategiaAcoes.Valor => "valor",
        SubEstrategiaAcoes.Dividendos => "dividendos",
        SubEstrategiaAcoes.Misto => "misto",
        _ => throw new ArgumentOutOfRangeException()
    };

    private static string MapFiis(SubEstrategiaFiis fiis) => fiis switch
    {
        SubEstrategiaFiis.Renda => "renda",
        SubEstrategiaFiis.Valorizacao => "valorizacao",
        SubEstrategiaFiis.Misto => "misto",
        _ => throw new ArgumentOutOfRangeException()
    };
}
