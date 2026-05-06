namespace landing_page_isis.core.Models.DTOs;

public record LeadListItemDto(
    Guid Id,
    string Name,
    string Email,
    string Phone,
    string Intent,
    DateOnly Created,
    LeadStatusEnum LeadStatus,
    bool PolicySigned
);
