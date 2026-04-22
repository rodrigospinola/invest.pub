namespace Invest.Application.Responses;

public record ChatResponse(
    string Response,
    List<ChatMessageResponse> History,
    List<string>? ToolsCalled,
    List<string>? SuggestedReplies,
    List<AllocationPreviewItem>? AllocationPreview
);

public record ChatMessageResponse(string Role, string Content);

public record AllocationPreviewItem(string Classe, decimal Percentual);
