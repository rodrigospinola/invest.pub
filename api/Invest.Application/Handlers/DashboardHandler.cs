using Invest.Application.Queries.Dashboard;
using Invest.Application.Responses;
using Invest.Domain.Enums;
using Invest.Domain.Interfaces;
using Invest.Domain.Services;

namespace Invest.Application.Handlers;

public class DashboardHandler
{
    private readonly IPortfolioHistoryRepository _portfolioHistoryRepository;
    private readonly IUserProfileRepository _profileRepository;
    private readonly IUserAssetRepository _assetRepository;
    private readonly IAlertRepository _alertRepository;
    private readonly IAssetHistoryRepository _assetHistoryRepository;
    private readonly IBenchmarkRepository _benchmarkRepository;
    private readonly DeviationCalculator _deviationCalculator;
    private readonly AllocationService _allocationService;

    public DashboardHandler(
        IPortfolioHistoryRepository portfolioHistoryRepository,
        IUserProfileRepository profileRepository,
        IUserAssetRepository assetRepository,
        IAlertRepository alertRepository,
        IAssetHistoryRepository assetHistoryRepository,
        IBenchmarkRepository benchmarkRepository,
        DeviationCalculator deviationCalculator,
        AllocationService allocationService)
    {
        _portfolioHistoryRepository = portfolioHistoryRepository;
        _profileRepository = profileRepository;
        _assetRepository = assetRepository;
        _alertRepository = alertRepository;
        _assetHistoryRepository = assetHistoryRepository;
        _benchmarkRepository = benchmarkRepository;
        _deviationCalculator = deviationCalculator;
        _allocationService = allocationService;
    }

    public async Task<DashboardResponse> GetDashboardAsync(GetDashboardQuery query)
    {
        var profile = await _profileRepository.GetByUserIdAsync(query.UserId);
        if (profile == null) throw new Exception("Perfil não encontrado");

        var assets = await _assetRepository.GetByUserIdAsync(query.UserId);
        var latestHistory = await _portfolioHistoryRepository.GetLatestByUserIdAsync(query.UserId);
        var alerts = await _alertRepository.GetUnreadByUserIdAsync(query.UserId);

        var currentValues = assets
            .GroupBy(a => a.Classe)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.Quantidade * a.PrecoMedio));

        var totalValue = currentValues.Values.Sum();
        var targetAllocation = _allocationService.ObterAlocacao(profile.Perfil, totalValue);
        var deviations = _deviationCalculator.Calculate(currentValues, targetAllocation);

        var meta = 500000m;
        var distanciaMeta = Math.Max(0, meta - totalValue);
        var percentualMeta = Math.Min(100, (totalValue / meta) * 100);

        return new DashboardResponse(
            totalValue,
            latestHistory?.RentabilidadeNoDia ?? 0,
            latestHistory?.RentabilidadeAcumulada ?? 0,
            distanciaMeta,
            Math.Round(percentualMeta, 2),
            deviations.Select(d => new DeviationResponse(
                d.Classe, d.Real, d.Alvo, d.Diferenca, d.DiferencaPercentual, d.IsAlertaExtraordinario)).ToList(),
            alerts.Take(5).Select(a => new AlertResponse(
                a.Id, a.Titulo, a.Mensagem, a.Tipo.ToString(), a.Status.ToString(), a.MetadataJson, a.CreatedAt)).ToList()
        );
    }

    public async Task<HistoryResponse> GetHistoryAsync(GetHistoryQuery query)
    {
        var history = await _portfolioHistoryRepository.GetByUserIdAsync(query.UserId, query.LastDays);
        var cdi = await _benchmarkRepository.GetByNameAsync("CDI", query.LastDays);
        var ibov = await _benchmarkRepository.GetByNameAsync("Ibovespa", query.LastDays);

        var points = history.Select(h => new HistoryPointResponse(h.Data, h.ValorTotal, h.RentabilidadeAcumulada)).ToList();
        
        var benchmarks = new List<BenchmarkResponse>
        {
            new BenchmarkResponse("CDI", cdi.Select(b => new BenchmarkPointResponse(b.Data, b.Valor, b.VariacaoNoDia)).ToList()),
            new BenchmarkResponse("Ibovespa", ibov.Select(b => new BenchmarkPointResponse(b.Data, b.Valor, b.VariacaoNoDia)).ToList())
        };

        return new HistoryResponse(points, benchmarks);
    }

    public async Task<List<AssetHistoryResponse>> GetAssetHistoryAsync(GetAssetHistoryQuery query)
    {
        var history = await _assetHistoryRepository.GetByTickerAsync(query.Ticker, query.LastDays);
        return history.Select(h => new AssetHistoryResponse(h.Data, h.PrecoFechamento, h.DividendoNoDia)).ToList();
    }
}
