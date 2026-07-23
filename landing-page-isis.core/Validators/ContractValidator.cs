using FluentValidation;
using landing_page_isis.core.Models;

namespace landing_page_isis.core.Validators;

public class ContractValidator : AbstractValidator<Contract>
{
    public ContractValidator()
    {
        RuleFor(c => c).NotNull().WithMessage("Dados do contrato inválidos.");

        RuleFor(c => c)
            .Must(c =>
                (c.PatientId.HasValue && c.PatientId.Value != Guid.Empty) || c.CoupleId.HasValue
            )
            .WithMessage("O contrato deve ser associado a um paciente individual ou casal.");

        RuleFor(c => c.Price)
            .NotNull()
            .WithMessage("O valor por sessão é obrigatório.")
            .GreaterThan(0)
            .WithMessage("O valor por sessão deve ser maior que zero.");
    }
}
