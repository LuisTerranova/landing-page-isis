namespace landing_page_isis.core.Models.DTOs;

public record CoupleListItemDto(
    Guid Id,
    string Name,
    string Partner1Name,
    string Partner2Name,
    string Partner1Phone,
    string? Partner1Email
);
