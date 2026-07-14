using System.ComponentModel.DataAnnotations;

namespace landing_page_isis.core.Models;

/// <summary>
/// Holds the personal details of a participant (patient) on a contract.
/// Used via composition in the Contract model.
/// </summary>
public class ContractParticipantInfo
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(150, ErrorMessage = "Nome deve ter no máximo 150 caracteres")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(14, ErrorMessage = "CPF inválido")]
    public string? Cpf { get; set; }

    [MaxLength(255, ErrorMessage = "E-mail deve ter no máximo 255 caracteres")]
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    public string? Email { get; set; }

    [MaxLength(11, ErrorMessage = "Telefone deve ter no máximo 11 dígitos")]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(2, ErrorMessage = "Estado deve ser a sigla de 2 letras")]
    public string? State { get; set; }

    public DateOnly? BirthDate { get; set; }

    // Cryptographic hash of the patient's CPF, used for duplicate checking without violating PII protection rules
    public string? CpfHash { get; set; }
}
