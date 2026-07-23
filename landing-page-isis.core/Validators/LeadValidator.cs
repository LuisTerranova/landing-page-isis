using System.Text.RegularExpressions;
using FluentValidation;
using landing_page_isis.core.Helpers;
using landing_page_isis.core.Models;

namespace landing_page_isis.core.Validators;

public partial class LeadValidator : AbstractValidator<Lead>
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    public LeadValidator()
    {
        RuleFor(l => l).NotNull().WithMessage("Dados não podem ser nulos.");

        RuleFor(l => l.Name)
            .NotEmpty()
            .WithMessage("Nome é obrigatório.")
            .MaximumLength(150)
            .WithMessage("Nome deve ter no máximo 150 caracteres.");

        RuleFor(l => l.Email)
            .Must(email => string.IsNullOrEmpty(email) || EmailRegex().IsMatch(email))
            .WithMessage("E-mail inválido.");

        RuleFor(l => l.Phone)
            .Must(phone =>
            {
                if (string.IsNullOrEmpty(phone))
                    return true;
                var stripped = CpfValidator.Strip(phone);
                return stripped.Length is >= 10 and <= 11;
            })
            .WithMessage("Telefone inválido. Deve ter 10 ou 11 dígitos.");
    }
}
