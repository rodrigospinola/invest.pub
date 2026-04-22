namespace Invest.Application.Queries.Chat;

public record ChatMessage(string Role, string Content);

public record SendChatMessageQuery(
    Guid UserId,
    string Message,
    string Context,
    List<ChatMessage>? History
);
