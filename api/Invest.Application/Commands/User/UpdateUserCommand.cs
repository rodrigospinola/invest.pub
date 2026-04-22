namespace Invest.Application.Commands.User;

public record UpdateUserCommand(
    Guid UserId,
    string Nome,
    string? Telefone
);
