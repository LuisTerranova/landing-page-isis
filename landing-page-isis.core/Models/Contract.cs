using System.ComponentModel.DataAnnotations;

namespace landing_page_isis.core.Models;

/// <summary>
/// Represents a service contract drafted for a patient or couple, tracking pricing, terms acceptance, and signature metadata.
/// </summary>
public class Contract
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(150, ErrorMessage = "Nome deve ter no máximo 150 caracteres")]
    public string PatientName { get; set; } = string.Empty;

    [MaxLength(14, ErrorMessage = "CPF inválido")]
    public string? PatientCpf { get; set; }

    [MaxLength(255, ErrorMessage = "E-mail deve ter no máximo 255 caracteres")]
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    public string? PatientEmail { get; set; }

    [RegularExpression(
        @"^\(?\d{2}\)?\s?\d{4,5}-?\d{4}$",
        ErrorMessage = "Telefone inválido. Use (XX) 9XXXX-XXXX"
    )]
    [MaxLength(11, ErrorMessage = "Telefone deve ter no máximo 11 dígitos")]
    public string PatientPhone { get; set; } = string.Empty;

    [MaxLength(2, ErrorMessage = "Estado deve ser a sigla de 2 letras")]
    public string? PatientState { get; set; }

    public DateOnly? PatientBirthDate { get; set; }

    public bool TermsAccepted { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public ContractStatus Status { get; set; } = ContractStatus.Rascunho;

    public decimal? Price { get; set; }

    public int? InitialAppointments { get; set; }

    public decimal? PackagePrice { get; set; }

    public string? AcceptanceToken { get; set; }

    public DateTimeOffset? TokenGeneratedAt { get; set; }

    public DateTimeOffset? AcceptedAt { get; set; }

    public string? ContractDocumentHtml { get; set; }

    public Guid? PackageId { get; set; }

    public AppointmentPackage? Package { get; set; }

    public Guid? PatientId { get; set; }

    // Cryptographic hash of the patient's CPF, used for duplicate checking without violating PII protection rules
    public string? PatientCpfHash { get; set; }

    public Patient? Patient { get; set; }

    public Guid? CoupleId { get; set; }

    public Couple? Couple { get; set; }

    public string? CoupleName { get; set; }

    [MaxLength(150)]
    public string? Patient2Name { get; set; }

    [MaxLength(14)]
    public string? Patient2Cpf { get; set; }

    [MaxLength(255)]
    public string? Patient2Email { get; set; }

    [MaxLength(11)]
    public string Patient2Phone { get; set; } = string.Empty;

    [MaxLength(2)]
    public string? Patient2State { get; set; }

    public DateOnly? Patient2BirthDate { get; set; }

    public string? Patient2CpfHash { get; set; }
}
