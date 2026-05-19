using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using RazorLight;

namespace landing_page_isis.Services;

public class EmailService(
    IServiceProvider services,
    IHttpClientFactory httpFactory,
    ILogger<EmailService> logger
) : BackgroundService
{
    private static readonly TimeZoneInfo BrTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
        "America/Sao_Paulo"
    );
    private static readonly TimeZoneInfo PvhTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
        "America/Porto_Velho"
    );
    private readonly TimeSpan _period = TimeSpan.FromHours(1);
    private readonly RazorLightEngine _razor = new RazorLightEngineBuilder()
        .UseFileSystemProject(Path.Combine(Directory.GetCurrentDirectory(), "Templates"))
        .UseMemoryCachingProvider()
        .Build();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EmailService started (Operating in Porto Velho Time).");

        while (!stoppingToken.IsCancellationRequested)
        {
            var nowInPvh = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PvhTimeZone);
            var currentHour = nowInPvh.Hour;

            // Only process between 08:00 and 18:00 PORTO VELHO time
            if (currentHour is >= 8 and < 18)
            {
                await ProcessReminders(stoppingToken);
                await Task.Delay(_period, stoppingToken);
            }
            else
            {
                // Calculate the time remaining until 08:00 the next day in Porto Velho
                var nextRun = nowInPvh.Date;

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

    private async Task ProcessReminders(CancellationToken ct)
    {
        try
        {
            using var scope = services.CreateScope();

            var appointmentHandler =
                scope.ServiceProvider.GetRequiredService<IAppointmentHandler>();
            var patientHandler =
                scope.ServiceProvider.GetRequiredService<IPatientHandler>();

            var nowInBr = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrTimeZone);
            var todayBrDate = nowInBr.Date;

            var startOfTodayBrInUtc = new DateTimeOffset(
                todayBrDate,
                BrTimeZone.GetUtcOffset(todayBrDate)
            ).ToUniversalTime();
            var endOfTomorrowBrInUtc = startOfTodayBrInUtc.AddDays(2).AddTicks(-1);

            var appointments = await appointmentHandler.GetAllAppointmentsByDateRange(
                startOfTodayBrInUtc,
                endOfTomorrowBrInUtc,
                ct
            );

            var toProcess = appointments
                .Where(a => a.AppointmentStatus == AppointmentStatusEnum.Marcada && !a.ReminderSent)
                .ToList();

            var patientIds = toProcess.Select(a => a.PatientId).Distinct();
            var emailMap = await patientHandler.GetPatientEmailMap(patientIds, ct);

            var eligible = toProcess
                .Where(a => emailMap.GetValueOrDefault(a.PatientId) != null)
                .ToList();

            logger.LogInformation(
                "Found {Total} appointments, {Eligible} have an email.",
                toProcess.Count,
                eligible.Count
            );

            foreach (var dto in eligible)
            {
                var appointment = await appointmentHandler.GetAppointmentWithPatient(dto.Id, dto.PatientId);
                if (appointment == null)
                    continue;

                var success = await SendAppointmentReminder(appointment, ct);
                if (success)
                {
                    appointment.ReminderSent = true;
                    await appointmentHandler.UpdateAppointment(appointment, appointment.Id);
                    logger.LogInformation(
                        "Lembrete enviado para consulta {Id}",
                        appointment.Id
                    );
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing email reminders");
        }
    }

    private async Task<bool> SendAppointmentReminder(Appointment appointment, CancellationToken ct)
    {
        try
        {
            if (appointment.Patient == null || string.IsNullOrEmpty(appointment.Patient.Email))
                return false;

            var html = await _razor.CompileRenderAsync("AppointmentReminder.cshtml", appointment);

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
