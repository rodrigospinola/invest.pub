using FluentValidation;
using Invest.Application.Commands.Portfolio;

namespace Invest.Application.Validators;

public class ImportPortfolioValidator : AbstractValidator<ImportPortfolioCommand>
{
    public ImportPortfolioValidator()
    {
        RuleFor(x => x.Ativos)
            .NotEmpty().WithMessage("A lista de ativos não pode estar vazia.");

        RuleForEach(x => x.Ativos).ChildRules(ativo =>
        {
            ativo.RuleFor(a => a.Ticker)
                .NotEmpty().WithMessage("Ticker é obrigatório.");

            ativo.RuleFor(a => a.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório.");

            ativo.RuleFor(a => a.Quantidade)
                .GreaterThan(0).WithMessage("Quantidade deve ser maior que zero.");

            ativo.RuleFor(a => a.PrecoMedio)
                .GreaterThan(0).WithMessage("Preço médio deve ser maior que zero.");
        });
    }
}
