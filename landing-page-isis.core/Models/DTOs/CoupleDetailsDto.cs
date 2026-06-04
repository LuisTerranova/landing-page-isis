namespace landing_page_isis.core.Models.DTOs;

public record CoupleDetailsDto(
    Guid Id,
    string Name,
    Patient Partner1,
    Patient Partner2,
    string? PayerName,
    string? PayerCpf,
    bool PolicySigned
);
