using System.ComponentModel.DataAnnotations;

namespace landing_page_isis.core.Models;

public class Patient
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(150, ErrorMessage = "Nome deve ter no máximo 150 caracteres")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(14, ErrorMessage = "CPF inválido")]
    public string? Cpf { get; set; }

    public DateOnly? BirthDate { get; set; }
    public int? Age => BirthDate.HasValue ? DateTime.Today.Year - BirthDate.Value.Year : null;

    [MaxLength(255, ErrorMessage = "E-mail deve ter no máximo 255 caracteres")]
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "O telefone e obrigatorio")]
    [RegularExpression(
        @"^\(?\d{2}\)?\s?\d{4,5}-?\d{4}$",
        ErrorMessage = "Telefone inválido. Use (XX) 9XXXX-XXXX"
    )]
    [MaxLength(11, ErrorMessage = "Telefone deve ter no máximo 11 dígitos")]
    public string Phone { get; set; } = string.Empty;

    public string? StateOfResidency { get; set; }
    public bool PolicySigned { get; set; } = false;

    [MaxLength(150, ErrorMessage = "Nome do pagador deve ter no máximo 150 caracteres")]
    public string? PayerName { get; set; }

    [MaxLength(14, ErrorMessage = "CPF do pagador inválido")]
    public string? PayerCpf { get; set; }

    public IEnumerable<Appointment>? Appointments { get; set; }

    public Contract? Contract { get; set; }
}
