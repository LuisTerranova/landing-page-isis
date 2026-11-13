namespace landing_page_isis.core.Models.DTOs;

/// <summary>
/// Represents a single entry in the unified clinical record timeline (appointment or standalone note).
/// </summary>
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
