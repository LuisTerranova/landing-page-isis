using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;

namespace landing_page_isis.core.Interfaces;

public interface IAppointmentHandler
{
    Task<PaginatedResponse<AppointmentListItemDto>> GetAllAppointments(
        int page,
        int pageSize,
        CancellationToken ct
    );
    Task<PaginatedResponse<AppointmentListItemDto>> GetAppointmentsByPatientId(
        int page,
        int pageSize,
        Guid patientId,
        CancellationToken ct
    );
    Task<Appointment?> GetAppointment(Guid id, Guid patientId);
    Task<HandlerResult> CreateAppointment(Appointment appointment);
    Task<HandlerResult> UpdateAppointment(Appointment appointment, Guid id);
    Task<HandlerResult> DeleteAppointment(Guid id);
    Task<List<AppointmentListItemDto>> GetAllAppointmentsByDateRange(
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct
    );
    Task<PaginatedResponse<AppointmentListItemDto>> GetAppointmentsByDateRange(
        DateTimeOffset start,
        DateTimeOffset end,
        int page,
        int pageSize,
        CancellationToken ct
    );
    Task<PaginatedResponse<AppointmentListItemDto>> QueryAppointments(
        string query,
        int page,
        int pageSize,
        CancellationToken ct
    );
}
