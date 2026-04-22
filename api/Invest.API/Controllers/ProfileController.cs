using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Invest.Application.Commands.Profile;
using Invest.Application.Handlers;
using Invest.Application.Queries.Profile;

namespace Invest.API.Controllers;

[ApiController]
[Route("profile")]
[Authorize]
public class ProfileController(ProfileHandler profileHandler) : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProfileRequest request) =>
        RespondCreated(await profileHandler.CreateProfileAsync(
            new CreateProfileCommand(UserId, request.Perfil, request.ValorTotal,
                request.TemCarteiraExistente, request.CarteiraAnterior)));

    [HttpGet]
    public async Task<IActionResult> Get() =>
        RespondOrNotFound(await profileHandler.GetProfileAsync(new GetProfileQuery(UserId)));

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest request) =>
        RespondOrNotFound(await profileHandler.UpdateProfileAsync(
            new UpdateProfileCommand(UserId, request.Perfil, request.ValorTotal)));

    /// <summary>Remove perfil, sub-estratégia e todos os ativos. O usuário volta ao onboarding.</summary>
    [HttpDelete("reset")]
    public async Task<IActionResult> Reset() =>
        Respond(await profileHandler.ResetProfileAsync(new ResetProfileCommand(UserId)));
}

public record CreateProfileRequest(
    string Perfil,
    decimal ValorTotal,
    bool TemCarteiraExistente,
    Dictionary<string, decimal>? CarteiraAnterior
);

public record UpdateProfileRequest(string? Perfil, decimal? ValorTotal);
