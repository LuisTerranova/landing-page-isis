using FluentValidation;
using landing_page_isis.core.Models;

namespace landing_page_isis.core.Validators;

/// <summary>
/// Validates standalone clinical notes, ensuring content, event date, and exactly one owner.
/// </summary>
public class AdditionalNoteValidator : AbstractValidator<AdditionalNote>
{
    public AdditionalNoteValidator()
    {
        RuleFor(n => n).NotNull().WithMessage("Dados da nota inválidos.");

        RuleFor(n => n.Content).NotEmpty().WithMessage("Conteúdo da nota é obrigatório.");

        RuleFor(n => n.NoteDate)
            .NotEqual(default(DateTimeOffset))
            .WithMessage("Data da nota é obrigatória.");

        RuleFor(n => n)
            .Must(n =>
                (n.PatientId.HasValue && n.PatientId.Value != Guid.Empty)
                != (n.CoupleId.HasValue && n.CoupleId.Value != Guid.Empty)
            )
            .WithMessage("A nota deve estar vinculada a exatamente um paciente ou casal.");
    }
}
