namespace Invest.Application.Commands.Profile;

public record UpdateProfileCommand(
    Guid UserId,
    string? Perfil,
    decimal? ValorTotal
);
