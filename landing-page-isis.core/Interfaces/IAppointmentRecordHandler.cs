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
}
