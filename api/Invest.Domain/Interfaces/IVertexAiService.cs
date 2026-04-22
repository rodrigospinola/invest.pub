namespace Invest.Domain.Interfaces;

public interface IVertexAiService
{
    Task<VertexAiResponse> SendMessageAsync(VertexAiRequest request);
}

public record VertexAiMessage(string Role, string Content);

public record VertexAiToolUse(string Id, string Name, object Input);

public record VertexAiToolResult(string ToolUseId, string Content);

public record VertexAiRequest(
    string SystemPrompt,
    List<VertexAiMessage> Messages,
    List<VertexAiToolDefinition>? Tools = null,
    int MaxTokens = 1024
);

public record VertexAiToolDefinition(
    string Name,
    string Description,
    object InputSchema
);

public record VertexAiResponse(
    string Text,
    List<VertexAiToolUse>? ToolUses,
    string StopReason
);
