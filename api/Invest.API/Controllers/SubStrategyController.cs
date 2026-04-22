using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Invest.Application.Commands.SubStrategy;
using Invest.Application.Handlers;
using Invest.Application.Queries.SubStrategy;

namespace Invest.API.Controllers;

[ApiController]
[Route("sub-strategy")]
[Authorize]
public class SubStrategyController(SubStrategyHandler subStrategyHandler) : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubStrategyRequest request) =>
        Respond(await subStrategyHandler.CreateSubStrategyAsync(
            new CreateSubStrategyCommand(UserId, request.SubEstrategiaAcoes, request.SubEstrategiaFiis)));

    [HttpGet]
    public async Task<IActionResult> Get() =>
        RespondOrNotFound(await subStrategyHandler.GetSubStrategyAsync(new GetSubStrategyQuery(UserId)));
}

public record CreateSubStrategyRequest(string SubEstrategiaAcoes, string SubEstrategiaFiis);
