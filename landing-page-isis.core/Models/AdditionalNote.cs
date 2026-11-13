namespace landing_page_isis.core.Models;

/// <summary>
/// Represents a standalone clinical note attached to a patient or a couple, independent of an appointment.
/// </summary>
public class AdditionalNote
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid? CoupleId { get; set; }
    public Couple? Couple { get; set; }

    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Date and time of the event informed by the user.
    /// </summary>
    public DateTimeOffset NoteDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
