using FluentValidation;
using Invest.Application.Commands.Auth;

namespace Invest.Application.Validators;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .EmailAddress().WithMessage("Email inválido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória.");
    }
}
