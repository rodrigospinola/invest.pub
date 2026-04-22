using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Invest.Application.Commands.User;
using Invest.Application.Handlers;
using Invest.Application.Queries.User;

namespace Invest.API.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public class UserController(UserHandler userHandler) : ApiControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe() =>
        RespondOrNotFound(await userHandler.GetUserAsync(new GetUserQuery(UserId)));

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMeRequest request) =>
        Respond(await userHandler.UpdateUserAsync(new UpdateUserCommand(UserId, request.Nome, request.Telefone)));

    [HttpDelete("me")]
    public async Task<IActionResult> DeactivateMe() =>
        RespondOrNotFound(await userHandler.DeactivateUserAsync(new DeactivateUserCommand(UserId)));
}

public record UpdateMeRequest(string Nome, string? Telefone);
