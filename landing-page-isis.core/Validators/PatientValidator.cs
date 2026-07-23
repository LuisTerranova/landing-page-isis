using System.Text.RegularExpressions;
using FluentValidation;
using landing_page_isis.core.Helpers;
using landing_page_isis.core.Models;

namespace landing_page_isis.core.Validators;

public partial class PatientValidator : AbstractValidator<Patient>
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    public PatientValidator()
    {
        RuleFor(p => p).NotNull().WithMessage("Dados inválidos.");

        RuleFor(p => p.Name)
            .NotEmpty()
            .WithMessage("Nome é obrigatório.")
            .MaximumLength(150)
            .WithMessage("Nome deve ter no máximo 150 caracteres.");

        RuleFor(p => p.Email)
            .Must(email => string.IsNullOrEmpty(email) || EmailRegex().IsMatch(email))
            .WithMessage("E-mail inválido.");

        RuleFor(p => p.Phone)
            .Must(phone =>
            {
                if (string.IsNullOrEmpty(phone))
                    return true;
                var stripped = CpfValidator.Strip(phone);
                return stripped.Length is >= 10 and <= 11;
            })
            .WithMessage("Telefone inválido. Deve ter 10 ou 11 dígitos.");

        RuleFor(p => p.Cpf)
            .Must(cpf =>
            {
                if (string.IsNullOrEmpty(cpf))
                    return true;
                var stripped = CpfValidator.Strip(cpf);
                return CpfValidator.IsValid(stripped);
            })
            .WithMessage("CPF inválido.");
    }
}
