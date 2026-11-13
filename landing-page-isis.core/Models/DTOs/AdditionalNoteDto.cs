namespace landing_page_isis.core.Models.DTOs;

/// <summary>
/// Represents a standalone clinical note for reading and editing.
/// </summary>
public record AdditionalNoteDto(
    Guid Id,
    Guid? PatientId,
    Guid? CoupleId,
    string Content,
    DateTimeOffset NoteDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
