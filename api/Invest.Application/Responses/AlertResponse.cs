namespace Invest.Application.Responses;

public record AlertResponse(
    Guid Id,
    string Titulo,
    string Mensagem,
    string Tipo,
    string Status,
    string? MetadataJson,
    DateTime CreatedAt
);
