using FluentValidation;
using landing_page_isis.core.Helpers;
using landing_page_isis.core.Models;

namespace landing_page_isis.core.Validators;

public class CoupleValidator : AbstractValidator<Couple>
{
    public CoupleValidator()
    {
        RuleFor(c => c).NotNull().WithMessage("Dados do casal não informados.");

        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage("Nome do casal é obrigatório.")
            .MaximumLength(150)
            .WithMessage("Nome do casal deve ter no máximo 150 caracteres.");

        RuleFor(c => c)
            .Must(c => c.Patient1Id != c.Patient2Id)
            .WithMessage("Os dois pacientes devem ser diferentes.");

        RuleFor(c => c.PayerCpf)
            .Must(cpf =>
            {
                if (string.IsNullOrEmpty(cpf))
                    return true;
                var stripped = CpfValidator.Strip(cpf);
                return stripped.Length == 11;
            })
            .WithMessage("CPF do pagador inválido. Deve ter 11 dígitos.");
    }
}
