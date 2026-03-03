using System.ComponentModel.DataAnnotations;

namespace landing_page_isis.core.Models;

public class Lead
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "O nome e obrigatorio")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "O email e obrigatorio")]
    [EmailAddress(ErrorMessage = "O email e invalido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "O telefone e obrigatorio")]
    [RegularExpression(
        @"^\(?\d{2}\)?\s?\d{4,5}-?\d{4}$",
        ErrorMessage = "Telefone inválido. Use (XX) 9XXXX-XXXX"
    )]
    public string Phone { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public DateOnly Created { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public LeadStatusEnum LeadStatus { get; set; } = LeadStatusEnum.Novo;
    public bool PolicySigned { get; set; } = false;
}
