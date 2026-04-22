using FluentValidation;
using Invest.Application.Commands.Portfolio;
using Invest.Application.Common;
using Invest.Application.Queries.Portfolio;
using Invest.Application.Responses;
using Invest.Domain.Entities;
using Invest.Domain.Enums;
using Invest.Domain.Interfaces;

namespace Invest.Application.Handlers;

public class PortfolioHandler
{
    private readonly IUserAssetRepository _assetRepository;
    private readonly IValidator<ImportPortfolioCommand> _importValidator;

    public PortfolioHandler(
        IUserAssetRepository assetRepository,
        IValidator<ImportPortfolioCommand> importValidator)
    {
        _assetRepository = assetRepository;
        _importValidator = importValidator;
    }

    public async Task<Result<PortfolioImportResponse>> ImportPortfolioAsync(ImportPortfolioCommand command)
    {
        var validation = await _importValidator.ValidateAsync(command);
        if (!validation.IsValid)
        {
            var error = validation.Errors.First();
            return Result<PortfolioImportResponse>.Failure("VALIDATION_ERROR", error.ErrorMessage, error.PropertyName);
        }

        var assets = new List<UserAsset>();
        var tickersFalharam = new List<string>();

        foreach (var ativo in command.Ativos)
        {
            if (!TryParseClasse(ativo.Classe, out var classe))
            {
                tickersFalharam.Add(ativo.Ticker);
                classe = ClasseAtivo.Acoes;
            }

            var asset = UserAsset.Create(
                command.UserId,
                portfolioDesignId: null,
                ativo.Ticker.ToUpper(),
                ativo.Nome,
                classe,
                subEstrategia: null,
                ativo.Quantidade,
                ativo.PrecoMedio,
                OrigemAtivo.Proprio
            );

            assets.Add(asset);
        }

        await _assetRepository.AddRangeAsync(assets);

        return Result<PortfolioImportResponse>.Success(new PortfolioImportResponse(
            TotalImportados: assets.Count,
            Sugeridos: 0,
            Proprios: assets.Count,
            TickersFalharam: tickersFalharam
        ));
    }

    public async Task<Result<AssetsResponse>> GetAssetsAsync(GetAssetsQuery query)
    {
        var assets = await _assetRepository.GetByUserIdAsync(query.UserId);

        var itens = assets.Select(a => new AssetItemResponse(
            a.Id,
            a.Ticker,
            a.Nome,
            a.Classe.ToString(),
            a.Quantidade,
            a.PrecoMedio,
            a.Origem.ToString().ToLower(),
            a.Ativo,
            a.CreatedAt
        )).ToList();

        return Result<AssetsResponse>.Success(new AssetsResponse(itens));
    }

    private static bool TryParseClasse(string value, out ClasseAtivo result)
    {
        if (Enum.TryParse<ClasseAtivo>(value, ignoreCase: true, out result))
            return true;

        // Try normalized string matching
        switch (value.ToLower().Replace("_", "").Replace(" ", ""))
        {
            case "rfdinamica": result = ClasseAtivo.RFDinamica; return true;
            case "rfpos": result = ClasseAtivo.RFPos; return true;
            case "fundosimobiliarios":
            case "fii":
            case "fiis": result = ClasseAtivo.FundosImobiliarios; return true;
            case "acoes":
            case "acao": result = ClasseAtivo.Acoes; return true;
            case "internacional": result = ClasseAtivo.Internacional; return true;
            case "fundosmultimercados":
            case "multimercados": result = ClasseAtivo.FundosMultimercados; return true;
            case "alternativos": result = ClasseAtivo.Alternativos; return true;
            default: result = ClasseAtivo.Acoes; return false;
        }
    }
}
