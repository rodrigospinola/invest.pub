using FluentValidation;
using Invest.Application.Commands.User;

namespace Invest.Application.Validators;

public class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(255).WithMessage("Nome deve ter no máximo 255 caracteres.");

        RuleFor(x => x.Telefone)
            .MaximumLength(20).WithMessage("Telefone deve ter no máximo 20 caracteres.")
            .When(x => x.Telefone != null);
    }
}
