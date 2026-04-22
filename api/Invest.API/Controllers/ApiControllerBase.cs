using Microsoft.AspNetCore.Mvc;
using Invest.Application.Common;
using System.Security.Claims;

namespace Invest.API.Controllers;

/// <summary>
/// Base para todas as controllers autenticadas.
/// Expõe <see cref="UserId"/> e helpers de resposta que eliminam o
/// boilerplate de if/else + construção manual do envelope de erro.
/// </summary>
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// ID do usuário autenticado extraído do JWT.
    /// Seguro de chamar em qualquer action protegida com [Authorize].
    /// </summary>
    protected Guid UserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

    // ── Helpers de resposta ───────────────────────────────────────────────────

    /// <summary>200 OK | 400 Bad Request</summary>
    protected IActionResult Respond<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : BadRequest(Err(result));

    /// <summary>201 Created | 400 Bad Request</summary>
    protected IActionResult RespondCreated<T>(Result<T> result) =>
        result.IsSuccess ? Created(string.Empty, result.Value) : BadRequest(Err(result));

    /// <summary>200 OK | 404 Not Found</summary>
    protected IActionResult RespondOrNotFound<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : NotFound(Err(result));

    /// <summary>200 OK | 401 Unauthorized</summary>
    protected IActionResult RespondOrUnauthorized<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : Unauthorized(Err(result));

    /// <summary>200 OK | 500 Internal Server Error</summary>
    protected IActionResult RespondOrServerError<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : StatusCode(500, Err(result));

    // ── Envelope de erro padrão ───────────────────────────────────────────────

    private static object Err<T>(Result<T> r) => new
    {
        error = new { code = r.ErrorCode, message = r.ErrorMessage, field = r.ErrorField }
    };
}
