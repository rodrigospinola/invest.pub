using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Invest.Application.Handlers;
using Invest.Application.Queries.Dashboard;

namespace Invest.API.Controllers;

[ApiController]
[Route("dashboard")]
[Authorize]
public class DashboardController(DashboardHandler dashboardHandler) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDashboard() =>
        Ok(await dashboardHandler.GetDashboardAsync(new GetDashboardQuery(UserId)));

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int lastDays = 30) =>
        Ok(await dashboardHandler.GetHistoryAsync(new GetHistoryQuery(UserId, lastDays)));

    [HttpGet("asset-history/{ticker}")]
    public async Task<IActionResult> GetAssetHistory(string ticker, [FromQuery] int lastDays = 30) =>
        Ok(await dashboardHandler.GetAssetHistoryAsync(new GetAssetHistoryQuery(ticker, lastDays)));
}
