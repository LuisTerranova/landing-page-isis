using System.ComponentModel.DataAnnotations;

namespace landing_page_isis.core.Models;

public class Pacient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public DateOnly? BirthDate { get; set; }
    public int? Age => BirthDate.HasValue ? DateTime.Today.Year - BirthDate.Value.Year : null;
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "O telefone e obrigatorio")]
    [RegularExpression(
        @"^\(?\d{2}\)?\s?\d{4,5}-?\d{4}$",
        ErrorMessage = "Telefone inválido. Use (XX) 9XXXX-XXXX"
    )]
    public string Phone { get; set; } = string.Empty;
    public string? StateOfResidency { get; set; }
    public bool PolicySigned { get; set; } = false;
    public IEnumerable<Appointment>? Appointments { get; set; }
}
