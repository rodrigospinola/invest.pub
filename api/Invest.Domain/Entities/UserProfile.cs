using Invest.Domain.Enums;
using System.Text.Json;

namespace Invest.Domain.Entities;

public class UserProfile
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public PerfilRisco Perfil { get; private set; }
    public decimal ValorTotal { get; private set; }
    public FaixaPatrimonio Faixa { get; private set; }
    public bool TemCarteiraExistente { get; private set; }
    public string? CarteiraAnteriorJson { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private UserProfile() { }

    public static UserProfile Create(
        Guid userId,
        PerfilRisco perfil,
        decimal valorTotal,
        FaixaPatrimonio faixa,
        bool temCarteiraExistente,
        Dictionary<string, decimal>? carteiraAnterior = null)
    {
        return new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Perfil = perfil,
            ValorTotal = valorTotal,
            Faixa = faixa,
            TemCarteiraExistente = temCarteiraExistente,
            CarteiraAnteriorJson = carteiraAnterior != null
                ? JsonSerializer.Serialize(carteiraAnterior)
                : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public bool Update(PerfilRisco? novoPerfil, decimal? novoValor, FaixaPatrimonio novaFaixa)
    {
        var mudouFaixa = novaFaixa != Faixa;
        Perfil = novoPerfil ?? Perfil;
        ValorTotal = novoValor ?? ValorTotal;
        Faixa = novaFaixa;
        UpdatedAt = DateTime.UtcNow;
        return mudouFaixa;
    }

    public Dictionary<string, decimal>? GetCarteiraAnterior() =>
        CarteiraAnteriorJson != null
            ? JsonSerializer.Deserialize<Dictionary<string, decimal>>(CarteiraAnteriorJson)
            : null;
}
