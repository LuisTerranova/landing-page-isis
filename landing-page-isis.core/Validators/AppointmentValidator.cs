using FluentValidation;
using landing_page_isis.core.Models;

namespace landing_page_isis.core.Validators;

public class AppointmentValidator : AbstractValidator<Appointment>
{
    public AppointmentValidator()
    {
        RuleFor(a => a).NotNull().WithMessage("Dados do agendamento inválidos.");

        RuleFor(a => a)
            .Must(a =>
                (a.PatientId.HasValue && a.PatientId.Value != Guid.Empty) || a.CoupleId.HasValue
            )
            .WithMessage("O agendamento deve estar vinculado a um Paciente ou Casal.");

        RuleFor(a => a.Price)
            .GreaterThanOrEqualTo(0)
            .WithMessage("O valor da sessão não pode ser negativo.");
    }
}
