using System.ComponentModel.DataAnnotations;

namespace landing_page_isis.core.Models;

public class Lead
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "O nome é obrigatório")]
    [MaxLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "O email é obrigatório")]
    [EmailAddress(ErrorMessage = "O email é inválido")]
    [MaxLength(150, ErrorMessage = "O email deve ter no máximo 150 caracteres")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "O telefone é obrigatório")]
    [RegularExpression(
        @"^\(?\d{2}\)?\s?\d{4,5}-?\d{4}$",
        ErrorMessage = "Telefone inválido. Use (XX) 9XXXX-XXXX"
    )]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "A mensagem deve ter no máximo 2000 caracteres")]
    public string Intent { get; set; } = string.Empty;
    public DateOnly Created { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public LeadStatusEnum LeadStatus { get; set; } = LeadStatusEnum.Novo;
    public bool PolicySigned { get; set; } = false;
}
