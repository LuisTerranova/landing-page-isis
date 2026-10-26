using AppointmentModel = landing_page_isis.core.Models.Appointment;

namespace landing_page_isis.Components.Dialogs.Appointment;

public sealed record AppointmentDialogResult(
    AppointmentModel Appointment,
    bool PackageCreated,
    bool IsCourtesy
);
