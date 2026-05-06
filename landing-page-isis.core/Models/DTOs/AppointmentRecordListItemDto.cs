namespace landing_page_isis.core.Models.DTOs;

public record AppointmentRecordListItemDto(
    Guid Id,
    Guid AppointmentId,
    DateTimeOffset? AppointmentDate,
    string Note,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
