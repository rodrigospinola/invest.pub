using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Invest.Application.Handlers;
using Invest.Application.Queries.Ranking;

namespace Invest.API.Controllers;

[ApiController]
[Route("ranking")]
[Authorize]
public class RankingController(RankingHandler rankingHandler) : ApiControllerBase
{
    [HttpGet("top20")]
    public async Task<IActionResult> GetTop20([FromQuery] string subEstrategia)
    {
        if (string.IsNullOrWhiteSpace(subEstrategia))
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "O parâmetro subEstrategia é obrigatório." } });

        return RespondOrNotFound(await rankingHandler.GetTop20Async(new GetTop20Query(subEstrategia)));
    }

    [HttpGet("suggestion")]
    public async Task<IActionResult> GetSuggestion() =>
        RespondOrNotFound(await rankingHandler.GetSuggestionAsync(new GetSuggestionQuery(UserId)));
}
