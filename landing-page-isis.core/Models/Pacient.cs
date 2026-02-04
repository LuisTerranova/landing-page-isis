using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace landing_page_isis.core.Models;

public class Pacient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    public string Cpf { get; set; }
    public DateOnly BirthDate { get; set; }
    public int Age => DateTime.Today.Year - BirthDate.Year;
    public string Email { get; set; }
    [Required(ErrorMessage = "O telefone e obrigatorio")]
    [RegularExpression(@"^\(?\d{2}\)?\s?\d{4,5}-?\d{4}$", ErrorMessage = "Telefone inválido. Use (XX) 9XXXX-XXXX")]
    public string Phone { get; set; }
    public string Address { get; set; }
    public IEnumerable<Appointment> Appointments { get; set; }
}
