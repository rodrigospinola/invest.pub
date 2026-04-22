using Invest.Domain.Enums;

namespace Invest.Domain.Entities;

public class UserSubStrategy
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public SubEstrategiaAcoes SubEstrategiaAcoes { get; private set; }
    public SubEstrategiaFiis SubEstrategiaFiis { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private UserSubStrategy() { }

    public static UserSubStrategy Create(Guid userId, SubEstrategiaAcoes acoes, SubEstrategiaFiis fiis)
    {
        return new UserSubStrategy
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SubEstrategiaAcoes = acoes,
            SubEstrategiaFiis = fiis,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(SubEstrategiaAcoes acoes, SubEstrategiaFiis fiis)
    {
        SubEstrategiaAcoes = acoes;
        SubEstrategiaFiis = fiis;
        UpdatedAt = DateTime.UtcNow;
    }
}
