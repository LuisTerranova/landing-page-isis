using landing_page_isis.core.Models;

namespace landing_page_isis.core.Interfaces;

public interface IAppointmentRecordHandler
{
    Task<AppointmentRecord?> GetAppointmentRecordById(Guid id);
    Task<AppointmentRecord?> GetAppointmentRecordByAppointmentId(Guid appointmentId);
    Task<HandlerResult> CreateAppointmentRecord(AppointmentRecord record);
    Task<HandlerResult> UpdateAppointmentRecord(AppointmentRecord record);
}