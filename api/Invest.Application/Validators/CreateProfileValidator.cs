using FluentValidation;
using Invest.Application.Commands.Profile;

namespace Invest.Application.Validators;

public class CreateProfileValidator : AbstractValidator<CreateProfileCommand>
{
    private static readonly string[] PerfisValidos = { "conservador", "moderado", "arrojado" };

    public CreateProfileValidator()
    {
        RuleFor(x => x.Perfil)
            .NotEmpty().WithMessage("Perfil é obrigatório.")
            .Must(p => PerfisValidos.Contains(p.ToLower()))
            .When(x => !string.IsNullOrEmpty(x.Perfil), ApplyConditionTo.CurrentValidator)
            .WithMessage("Perfil inválido. Valores aceitos: conservador, moderado, arrojado.");

        RuleFor(x => x.ValorTotal)
            .GreaterThan(0).WithMessage("Valor total deve ser maior que zero.");
    }
}
