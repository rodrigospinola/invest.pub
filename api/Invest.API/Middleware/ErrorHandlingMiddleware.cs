using System.Text.Json;

namespace Invest.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado");
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var response = new { error = new { code = "INTERNAL_ERROR", message = "Erro interno do servidor." } };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
