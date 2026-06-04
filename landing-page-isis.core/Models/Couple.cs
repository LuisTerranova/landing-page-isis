using System.ComponentModel.DataAnnotations;

namespace landing_page_isis.core.Models;

public class Couple
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "Nome do casal é obrigatório")]
    [MaxLength(150, ErrorMessage = "Nome deve ter no máximo 150 caracteres")]
    public string Name { get; set; } = string.Empty;

    public Guid Patient1Id { get; set; }
    public Guid Patient2Id { get; set; }

    [MaxLength(150, ErrorMessage = "Nome do pagador deve ter no máximo 150 caracteres")]
    public string? PayerName { get; set; }

    [MaxLength(14, ErrorMessage = "CPF do pagador inválido")]
    public string? PayerCpf { get; set; }

    public bool PolicySigned { get; set; } = false;

    public Patient Patient1 { get; set; } = null!;
    public Patient Patient2 { get; set; } = null!;
    public ICollection<Appointment>? Appointments { get; set; }
    public ICollection<AppointmentPackage>? Packages { get; set; }
}
