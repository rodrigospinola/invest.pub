using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Invest.Application.Commands.Alert;
using Invest.Application.Handlers;
using Invest.Application.Queries.Alert;

namespace Invest.API.Controllers;

[ApiController]
[Route("alerts")]
[Authorize]
public class AlertController(AlertHandler alertHandler) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAlerts() =>
        Ok(await alertHandler.GetAlertsAsync(new GetAlertsQuery(UserId)));

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id) =>
        Respond(await alertHandler.MarkAsReadAsync(new MarkAlertReadCommand(id, UserId)));
}
