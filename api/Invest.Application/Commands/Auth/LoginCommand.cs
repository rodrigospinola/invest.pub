namespace Invest.Application.Commands.Auth;

public record LoginCommand(
    string Email,
    string Password
);
