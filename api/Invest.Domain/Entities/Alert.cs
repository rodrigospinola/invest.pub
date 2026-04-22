using Invest.Domain.Enums;
using System.Text.Json;

namespace Invest.Domain.Entities;

public class Alert
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string Mensagem { get; private set; } = string.Empty;
    public AlertType Tipo { get; private set; }
    public AlertStatus Status { get; private set; }
    public string? MetadataJson { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }

    private Alert() { }

    public static Alert Create(
        Guid userId,
        string titulo,
        string mensagem,
        AlertType tipo,
        object? metadata = null)
    {
        return new Alert
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Titulo = titulo,
            Mensagem = mensagem,
            Tipo = tipo,
            Status = AlertStatus.Unread,
            MetadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsRead()
    {
        Status = AlertStatus.Read;
        ReadAt = DateTime.UtcNow;
    }

    public T? GetMetadata<T>() =>
        MetadataJson != null ? JsonSerializer.Deserialize<T>(MetadataJson) : default;
}
