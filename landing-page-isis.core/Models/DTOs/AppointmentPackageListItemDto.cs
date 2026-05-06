namespace landing_page_isis.core.Models.DTOs;

public record AppointmentPackageListItemDto(
    Guid Id,
    Guid PatientId,
    int TotalAppointments,
    int RemainingAppointments,
    decimal Price,
    PackageStatus Status,
    DateTimeOffset CreatedAt,
    PaymentMethod PaymentMethod
);
