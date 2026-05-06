namespace landing_page_isis.core.Models.DTOs;

public record PatientListItemDto(
    Guid Id,
    string Name,
    string? Email,
    string Phone,
    string? StateOfResidency
);
