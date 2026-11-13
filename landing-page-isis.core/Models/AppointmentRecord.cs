namespace landing_page_isis.core.Models;

public class AppointmentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AppointmentId { get; set; }
    public string? Note { get; set; }

    /// <summary>
    /// Indicates that the note is still an automatic draft and has not been finalized yet.
    /// </summary>
    public bool IsDraft { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public Appointment? Appointment { get; set; }
}
