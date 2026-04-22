namespace Invest.Application.Commands.Profile;

public record CreateProfileCommand(
    Guid UserId,
    string Perfil,
    decimal ValorTotal,
    bool TemCarteiraExistente,
    Dictionary<string, decimal>? CarteiraAnterior
);
