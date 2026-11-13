namespace landing_page_isis.core.Models.DTOs;

/// <summary>
/// Represents an upcoming birthday for an active patient.
/// </summary>
public record BirthdayListItemDto(
    Guid PatientId,
    string Name,
    DateOnly BirthDate,
    DateOnly NextBirthday,
    int DaysUntil,
    bool IsToday
);
