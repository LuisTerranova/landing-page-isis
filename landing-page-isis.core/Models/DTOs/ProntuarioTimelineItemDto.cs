namespace landing_page_isis.core.Models.DTOs;

/// <summary>
/// Represents a single entry in the unified clinical record timeline (appointment or standalone note).
/// </summary>
/// <remarks>
/// <c>UpdatedAt</c> holds the last rectification timestamp for consultation records
/// (mapped from <c>AppointmentRecord.RectifiedAt</c>) and the last edit timestamp for
/// standalone notes (mapped from <c>AdditionalNote.UpdatedAt</c>).
/// </remarks>
public record ProntuarioTimelineItemDto(
    Guid Id,
    ProntuarioItemType Type,
    DateTimeOffset EventDate,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    Guid? AppointmentId,
    bool IsDraft
);
