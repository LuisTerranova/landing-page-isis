using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using RazorLight;

namespace landing_page_isis.Services;

/// <summary>
/// Background service that periodically checks for upcoming appointments and sends email reminders to patients.
/// </summary>
public class EmailService(
    IServiceProvider services,
    IHttpClientFactory httpFactory,
    RazorLightEngine razor,
    ILogger<EmailService> logger
) : BackgroundService
{
    // Brasilia Time is used as the standard reference for date boundaries (today/tomorrow).
    private static readonly TimeZoneInfo BrTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
        "America/Sao_Paulo"
    );

    // Porto Velho Time is the local clinic time, used to enforce business hours so reminders aren't sent late at night.
    private static readonly TimeZoneInfo PvhTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
        "America/Porto_Velho"
    );

    // Determines the interval for checking upcoming appointments during business hours.
    private readonly TimeSpan _period = TimeSpan.FromHours(1);

    /// <summary>
    /// Executes the background service, running the reminder loop continuously during Porto Velho business hours.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EmailService started (Operating in Porto Velho Time).");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Convert current UTC time to Porto Velho local time
            var nowInPvh = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PvhTimeZone);
            var currentHour = nowInPvh.Hour;

            // Only process reminders between 08:00 and 18:00 Porto Velho time to avoid disturbing patients
            if (currentHour is >= 8 and < 18)
            {
                await ProcessReminders(stoppingToken);
                await Task.Delay(_period, stoppingToken);
            }
            else
            {
                // Calculate the time remaining until 08:00 local time
                var nextRun = nowInPvh.Date;

                // If it's past 18:00, schedule for 08:00 tomorrow morning
                if (currentHour >= 18)
                    nextRun = nextRun.AddDays(1);

                nextRun = nextRun.AddHours(8);

                var delay = nextRun - nowInPvh;
                logger.LogInformation(
                    "Fora do horário comercial de PVH ({NowPvh}). Aguardando {Delay} até as 08:00 PVH.",
                    nowInPvh,
                    delay
                );

                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    /// <summary>
    /// Retrieves scheduled appointments for today and tomorrow, checks if reminders are pending, and sends them out.
    /// </summary>
    private async Task ProcessReminders(CancellationToken ct)
    {
        try
        {
            using var scope = services.CreateScope();

            var appointmentHandler =
                scope.ServiceProvider.GetRequiredService<IAppointmentHandler>();
            var patientHandler = scope.ServiceProvider.GetRequiredService<IPatientHandler>();

            // Get current date/time in Brasilia timezone as standard reference
            var nowInBr = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrTimeZone);
            var todayBrDate = nowInBr.Date;

            // Determine boundaries in UTC to match database storage representation
            var startOfTodayBrInUtc = new DateTimeOffset(
                todayBrDate,
                BrTimeZone.GetUtcOffset(todayBrDate)
            ).ToUniversalTime();
            var endOfTomorrowBrInUtc = startOfTodayBrInUtc.AddDays(2).AddTicks(-1);

            // Fetch all appointments falling into the today/tomorrow range
            var appointments = await appointmentHandler.GetAllAppointmentsByDateRange(
                startOfTodayBrInUtc,
                endOfTomorrowBrInUtc,
                ct
            );

            // Filter for scheduled appointments where a reminder has not yet been sent
            var toProcess = appointments
                .Where(a => a.AppointmentStatus == AppointmentStatusEnum.Marcada && !a.ReminderSent)
                .ToList();

            // Bulk-load patient emails to prevent N+1 database queries when checking eligibility
            var patientIds = toProcess
                .Where(a => a.PatientId.HasValue)
                .Select(a => a.PatientId!.Value)
                .Distinct();
            var emailMap = await patientHandler.GetPatientEmailMap(patientIds, ct);

            // Keep only appointments that have an associated patient email
            var eligible = toProcess
                .Where(a =>
                    a.PatientId.HasValue && emailMap.GetValueOrDefault(a.PatientId.Value) != null
                )
                .ToList();

            logger.LogInformation(
                "Found {Total} appointments, {Eligible} have an email.",
                toProcess.Count,
                eligible.Count
            );

            // Send reminders and update the DB to prevent duplicate emails
            foreach (var dto in eligible)
            {
                var appointment = await appointmentHandler.GetAppointmentWithPatient(dto.Id, dto.PatientId!.Value);
                if (appointment == null)
                    continue;

                var success = await SendAppointmentReminder(appointment, ct);
                if (success)
                {
                    // Mark reminder as sent so it won't be picked up in subsequent executions
                    appointment.ReminderSent = true;
                    await appointmentHandler.UpdateAppointment(appointment, appointment.Id);
                    logger.LogInformation("Lembrete enviado para consulta {Id}", appointment.Id);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing email reminders");
        }
    }

    /// <summary>
    /// Compiles the reminder template and sends the email via Resend API.
    /// </summary>
    private async Task<bool> SendAppointmentReminder(Appointment appointment, CancellationToken ct)
    {
        try
        {
            if (appointment.Patient == null || string.IsNullOrEmpty(appointment.Patient.Email))
                return false;

            // Render template dynamically using RazorLight with appointment metadata
            var html = await razor.CompileRenderAsync("AppointmentReminder.cshtml", appointment);

            var http = httpFactory.CreateClient("resend");
            var response = await http.PostAsJsonAsync(
                "emails",
                new
                {
                    from = $"Isis Vitória <{Environment.GetEnvironmentVariable("RESEND_SENDER_EMAIL") ?? "onboarding@resend.dev"}>",
                    to = new[] { appointment.Patient.Email },
                    subject = "Lembrete de Sessão - Psicoterapia",
                    html,
                },
                ct
            );

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending reminder to {Email}", appointment.Patient?.Email);
            return false;
        }
    }
}
