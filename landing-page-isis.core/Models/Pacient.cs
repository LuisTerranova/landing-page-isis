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
    public string Phone { get; set; }
    public string Address { get; set; }
    [JsonIgnore]
    public IEnumerable<Appointment> Appointments { get; set; }
}
