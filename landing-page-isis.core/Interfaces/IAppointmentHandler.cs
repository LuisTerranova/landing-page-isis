using landing_page_isis.core.Models;

namespace landing_page_isis.core.Interfaces;

public interface IAppointmentHandler
{
    Task<PaginatedResponse<Appointment?>> GetAllAppointments(int page, int pageSize, CancellationToken ct);
    Task<PaginatedResponse<Appointment?>> GetAppointmentsByPacientId(int page, int pageSize, Guid pacientId, CancellationToken ct);
    Task<Appointment?> GetAppointment(Guid id, Guid pacientId);
    Task<HandlerResult> CreateAppointment(Appointment appointment);
    Task<HandlerResult> UpdateAppointment(Appointment appointment, Guid id);
    Task<HandlerResult> DeleteAppointment(Guid id);
}