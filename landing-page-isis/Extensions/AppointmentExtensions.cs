using landing_page_isis.core;
using landing_page_isis.core.Models;

namespace landing_page_isis.Extensions;

/// <summary>
/// Provides extension methods for validating and operating on the Appointment domain model.
/// </summary>
public static class AppointmentExtensions
{
    public static HandlerResult Validate(this Appointment appointment)
    {
        if (appointment == null)
            return new HandlerResult(false, "Dados inválidos.");

        // Prevent invalid negative prices for therapy sessions
        if (appointment.Price < 0)
            return new HandlerResult(false, "O preço não pode ser negativo.");

        // An appointment must be linked to either a single Patient or a Couple
        if (
            (!appointment.PatientId.HasValue || appointment.PatientId.Value == Guid.Empty)
            && appointment.CoupleId == null
        )
            return new HandlerResult(false, "Selecione um paciente ou casal válido.");

        return new HandlerResult(true);
    }
}
