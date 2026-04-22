using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Invest.Application.Handlers;
using Invest.Application.Queries.Chat;

namespace Invest.API.Controllers;

[ApiController]
[Route("chat")]
[Authorize]
public class ChatController(ChatHandler chatHandler) : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request) =>
        RespondOrServerError(await chatHandler.SendMessageAsync(new SendChatMessageQuery(
            UserId,
            request.Message,
            request.Context,
            request.History?.Select(h => new ChatMessage(h.Role, h.Content)).ToList())));
}

public record ChatHistoryItem(string Role, string Content);

public record ChatRequest(
    string Message,
    string Context,
    List<ChatHistoryItem>? History
);
