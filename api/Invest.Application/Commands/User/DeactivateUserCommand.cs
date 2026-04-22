namespace Invest.Application.Commands.User;

public record DeactivateUserCommand(
    Guid UserId
);
