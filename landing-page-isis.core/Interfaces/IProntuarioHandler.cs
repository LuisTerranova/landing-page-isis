using landing_page_isis.core.Models.DTOs;

namespace landing_page_isis.core.Interfaces;

/// <summary>
/// Provides the unified clinical record timeline, merging appointment records and standalone notes.
/// </summary>
public interface IProntuarioHandler
{
    /// <summary>
    /// Returns a paginated timeline for an individual patient, including drafts.
    /// </summary>
    Task<PaginatedResponse<ProntuarioTimelineItemDto>> GetTimelineByPatientId(
        int page,
        int pageSize,
        Guid patientId,
        DateTime? filterMonthYear,
        CancellationToken ct
    );

    /// <summary>
    /// Returns a paginated timeline for a couple, including drafts and both partners' appointments.
    /// </summary>
    Task<PaginatedResponse<ProntuarioTimelineItemDto>> GetTimelineByCoupleId(
        int page,
        int pageSize,
        Guid coupleId,
        DateTime? filterMonthYear,
        CancellationToken ct
    );

    /// <summary>
    /// Returns the complete timeline for an individual patient. Used by exports, which never include drafts.
    /// </summary>
    Task<IReadOnlyList<ProntuarioTimelineItemDto>> GetFullTimelineByPatientId(
        Guid patientId,
        bool includeNotes,
        bool includeDrafts,
        CancellationToken ct
    );

    /// <summary>
    /// Returns the complete timeline for a couple. Used by exports, which never include drafts.
    /// </summary>
    Task<IReadOnlyList<ProntuarioTimelineItemDto>> GetFullTimelineByCoupleId(
        Guid coupleId,
        bool includeNotes,
        bool includeDrafts,
        CancellationToken ct
    );
}
