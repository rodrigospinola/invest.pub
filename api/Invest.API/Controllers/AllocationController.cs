using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Invest.Application.Handlers;
using Invest.Application.Queries.Profile;

namespace Invest.API.Controllers;

[ApiController]
[Route("allocation")]
[Authorize]
public class AllocationController(ProfileHandler profileHandler) : ApiControllerBase
{
    [HttpGet]
    public IActionResult Get([FromQuery] string perfil, [FromQuery] decimal valor) =>
        Respond(profileHandler.GetAllocation(new GetAllocationQuery(perfil, valor)));
}
