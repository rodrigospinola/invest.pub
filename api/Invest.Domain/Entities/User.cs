using Invest.Domain.Enums;

namespace Invest.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? Telefone { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public UserStatus Status { get; private set; }
    public OnboardingStep OnboardingStep { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiresAt { get; private set; }

    private User() { }

    public static User Create(string nome, string email, string passwordHash, string? telefone = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Email = email,
            PasswordHash = passwordHash,
            Telefone = telefone,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = UserStatus.Ativo,
            OnboardingStep = OnboardingStep.Modelagem1
        };
    }

    public void Update(string nome, string? telefone)
    {
        Nome = nome;
        Telefone = telefone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRefreshToken(string token, DateTime expiresAt)
    {
        RefreshToken = token;
        RefreshTokenExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiresAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = UserStatus.Inativo;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasValidRefreshToken(string token) =>
        RefreshToken == token && RefreshTokenExpiresAt > DateTime.UtcNow;
}
