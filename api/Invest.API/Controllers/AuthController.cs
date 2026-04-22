using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Invest.Application.Commands.Auth;
using Invest.Application.Handlers;
using Invest.Application.Responses;

namespace Invest.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(AuthHandler authHandler, IWebHostEnvironment env) : ApiControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await authHandler.RegisterAsync(command);
        if (!result.IsSuccess)
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage, field = result.ErrorField } });

        SetRefreshTokenCookie(result.Value!.RefreshToken);
        return Created(string.Empty, ToPublicResponse(result.Value!));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await authHandler.LoginAsync(command);
        if (!result.IsSuccess)
            return Unauthorized(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });

        SetRefreshTokenCookie(result.Value!.RefreshToken);
        return Ok(ToPublicResponse(result.Value!));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { error = new { code = "MISSING_REFRESH_TOKEN", message = "Refresh token não encontrado." } });

        var result = await authHandler.RefreshTokenAsync(new RefreshTokenCommand(refreshToken));
        if (!result.IsSuccess)
            return Unauthorized(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });

        SetRefreshTokenCookie(result.Value!.RefreshToken);
        return Ok(ToPublicResponse(result.Value!));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command) =>
        Ok((await authHandler.ForgotPasswordAsync(command)).Value);

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command) =>
        RespondOrServerError(await authHandler.ResetPasswordAsync(command));

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var result = await authHandler.LogoutAsync(UserId);
        ClearRefreshTokenCookie();
        return Ok(result.Value);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    // Remove o refreshToken do payload público — ele trafega apenas via cookie httpOnly
    private static object ToPublicResponse(AuthResponse r) =>
        new { r.AccessToken, r.ExpiresAt, r.User };

    private void SetRefreshTokenCookie(string token) =>
        Response.Cookies.Append("refresh_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure   = !env.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires  = DateTimeOffset.UtcNow.AddDays(30),
        });

    private void ClearRefreshTokenCookie() =>
        Response.Cookies.Delete("refresh_token");
}
