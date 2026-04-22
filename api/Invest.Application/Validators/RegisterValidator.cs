using FluentValidation;
using Invest.Application.Commands.Auth;

namespace Invest.Application.Validators;

public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(255).WithMessage("Nome deve ter no máximo 255 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .EmailAddress().WithMessage("Email inválido.")
            .MaximumLength(255).WithMessage("Email deve ter no máximo 255 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(8).WithMessage("Senha deve ter no mínimo 8 caracteres.");
    }
}
