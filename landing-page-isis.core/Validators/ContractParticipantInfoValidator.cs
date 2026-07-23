using System.Text.RegularExpressions;
using FluentValidation;
using landing_page_isis.core.Helpers;
using landing_page_isis.core.Models;

namespace landing_page_isis.core.Validators;

public partial class ContractParticipantInfoValidator : AbstractValidator<ContractParticipantInfo>
{
    private static readonly string[] ValidUfs =
    [
        "AC",
        "AL",
        "AP",
        "AM",
        "BA",
        "CE",
        "DF",
        "ES",
        "GO",
        "MA",
        "MT",
        "MS",
        "MG",
        "PA",
        "PB",
        "PR",
        "PE",
        "PI",
        "RJ",
        "RN",
        "RS",
        "RO",
        "RR",
        "SC",
        "SP",
        "SE",
        "TO",
    ];

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    public ContractParticipantInfoValidator()
    {
        RuleFor(p => p).NotNull().WithMessage("Dados do participante não informados.");

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

        RuleFor(p => p.State)
            .Must(uf => string.IsNullOrEmpty(uf) || ValidUfs.Contains(uf.ToUpperInvariant()))
            .WithMessage("Estado (UF) inválido.");

        RuleFor(p => p.BirthDate)
            .Must(date => !date.HasValue || date.Value <= DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Data de nascimento inválida.");
    }
}
