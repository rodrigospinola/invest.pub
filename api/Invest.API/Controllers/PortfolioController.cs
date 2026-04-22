using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Invest.Application.Commands.Portfolio;
using Invest.Application.Commands.Profile;
using Invest.Application.Handlers;
using Invest.Application.Queries.Portfolio;

namespace Invest.API.Controllers;

[ApiController]
[Route("portfolio")]
[Authorize]
public class PortfolioController(
    ProfileHandler profileHandler,
    PortfolioHandler portfolioHandler,
    B3ImportHandler b3ImportHandler) : ApiControllerBase
{
    [HttpPost("compare")]
    public async Task<IActionResult> Compare([FromBody] CompareRequest request) =>
        Respond(await profileHandler.ComparePortfolioAsync(
            new ComparePortfolioCommand(UserId, request.CarteiraAtual)));

    [HttpPost("import/b3")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportB3(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = new { code = "INVALID_FILE", message = "Nenhum arquivo enviado ou arquivo vazio." } });

        return Respond(await b3ImportHandler.HandleAsync(new ImportB3ExcelCommand(UserId, file)));
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] ImportPortfolioRequest request) =>
        Respond(await portfolioHandler.ImportPortfolioAsync(
            new ImportPortfolioCommand(UserId, request.Ativos
                .Select(a => new ImportPortfolioAsset(a.Ticker, a.Nome, a.Classe, a.Quantidade, a.PrecoMedio))
                .ToList())));

    [HttpGet("assets")]
    public async Task<IActionResult> GetAssets() =>
        Respond(await portfolioHandler.GetAssetsAsync(new GetAssetsQuery(UserId)));
}

public record CompareRequest(Dictionary<string, decimal> CarteiraAtual);

public record ImportPortfolioAssetRequest(
    string Ticker,
    string Nome,
    string Classe,
    decimal Quantidade,
    decimal PrecoMedio
);

public record ImportPortfolioRequest(List<ImportPortfolioAssetRequest> Ativos);
