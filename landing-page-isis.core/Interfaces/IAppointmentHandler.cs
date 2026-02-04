using landing_page_isis.core.Models;

namespace landing_page_isis.core.Interfaces;

public interface IAppointmentHandler
{
    Task<PaginatedResponse<Appointment?>> GetAppointments(int page, int pageSize, Guid pacientId);
    Task<Appointment?> GetAppointment(Guid id, Guid pacientId);
    Task<HandlerResult> CreateAppointment(Appointment appointment);
    Task<HandlerResult> UpdateAppointment(Appointment appointment, Guid id);
    Task<HandlerResult> DeleteAppointment(Guid id);
}