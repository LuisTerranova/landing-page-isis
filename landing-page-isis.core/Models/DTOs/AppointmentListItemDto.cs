namespace landing_page_isis.core.Models.DTOs;

public record AppointmentListItemDto(
    Guid Id,
    DateTimeOffset AppointmentDate,
    Guid PatientId,
    string? PatientName,
    AppointmentStatusEnum AppointmentStatus,
    decimal Price,
    bool ReminderSent,
    Guid? PackageId
);
