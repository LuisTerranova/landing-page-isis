using landing_page_isis.core.Models;

namespace landing_page_isis.core.Interfaces;

public interface IAppointmentRecordHandler
{
    Task<AppointmentRecord?> GetAppointmentRecordById(Guid id);
    Task<HandlerResult> CreateAppointmentRecord(AppointmentRecord record);
    Task<HandlerResult> UpdateAppointmentRecord(AppointmentRecord record);
    Task<PaginatedResponse<AppointmentRecord?>> GetRecordsByPacientId(int page, int pageSize, Guid pacientId, DateTime? filterMonthYear, CancellationToken ct);
}