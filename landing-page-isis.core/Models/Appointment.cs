namespace landing_page_isis.core.Models;

public class Appointment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset AppointmentDate { get; set; }
    public Guid PacientId { get; set; }
    public Pacient? Pacient { get; set; }
    public AppointmentStatusEnum AppointmentStatus { get; set; } = AppointmentStatusEnum.Marcada;
    public decimal Price { get; set; }
    public bool ReminderSent { get; set; } = false;
    public AppointmentRecord? Record { get; set; }
}
