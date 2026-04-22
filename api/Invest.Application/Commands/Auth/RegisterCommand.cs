namespace Invest.Application.Commands.Auth;

public record RegisterCommand(
    string Nome,
    string Email,
    string Password,
    string? Telefone
);
