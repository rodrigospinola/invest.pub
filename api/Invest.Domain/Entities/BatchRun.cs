namespace Invest.Domain.Entities;

public class BatchRun
{
    public Guid Id { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private BatchRun() { }

    public static BatchRun Iniciar()
    {
        return new BatchRun
        {
            Id = Guid.NewGuid(),
            Status = "running",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Completar()
    {
        Status = "completed";
        CompletedAt = DateTime.UtcNow;
    }

    public void Falhar(string erro)
    {
        Status = "failed";
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = erro;
    }
}
