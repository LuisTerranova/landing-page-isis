using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;

namespace landing_page_isis.core.Interfaces;

public interface IAppointmentRecordHandler
{
    Task<AppointmentRecord?> GetAppointmentRecordById(Guid id);
    Task<HandlerResult> CreateAppointmentRecord(AppointmentRecord record);
    Task<HandlerResult> UpdateAppointmentRecord(AppointmentRecord record);
    Task<PaginatedResponse<AppointmentRecordListItemDto>> GetRecordsByPatientId(
        int page,
        int pageSize,
        Guid patientId,
        DateTime? filterMonthYear,
        CancellationToken ct
    );
    Task<AppointmentRecord?> GetAppointmentRecordByAppointmentId(Guid appointmentId);

    /// <summary>
    /// Returns the automatic draft associated with an appointment, when one exists.
    /// </summary>
    Task<AppointmentRecord?> GetDraftByAppointmentId(Guid appointmentId, CancellationToken ct);

    /// <summary>
    /// Creates or updates the automatic draft for an appointment. An empty note removes any existing draft.
    /// </summary>
    Task<HandlerResult> SaveDraft(Guid appointmentId, string note, CancellationToken ct);
}
