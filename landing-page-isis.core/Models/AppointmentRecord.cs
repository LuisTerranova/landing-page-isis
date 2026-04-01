namespace landing_page_isis.core.Models;

public class AppointmentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required Guid AppointmentId { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public Appointment? Appointment { get; set; }
}