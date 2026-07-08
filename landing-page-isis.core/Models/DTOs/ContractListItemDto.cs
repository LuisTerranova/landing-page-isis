namespace landing_page_isis.core.Models.DTOs;

public record ContractListItemDto(
    Guid Id,
    string FormId,
    string PatientName,
    ContractStatus Status,
    ContractType? Type,
    decimal? Price,
    DateTimeOffset CreatedAt,
    Guid? PatientId = null,
    Guid? CoupleId = null,
    string? CoupleName = null
);
