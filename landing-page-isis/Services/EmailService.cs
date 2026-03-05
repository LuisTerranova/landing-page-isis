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
    private readonly TimeSpan _period = TimeSpan.FromHours(1);
    private readonly RazorLightEngine _razor = new RazorLightEngineBuilder()
        .UseFileSystemProject(Path.Combine(Directory.GetCurrentDirectory(), "Templates"))
        .UseMemoryCachingProvider()
        .Build();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EmailService started. Executing initial check...");
        await ProcessReminders(stoppingToken);
        using PeriodicTimer timer = new(_period);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessReminders(stoppingToken);
        }
    }

    private async Task ProcessReminders(CancellationToken ct)
    {
        try
        {
            using var scope = services.CreateScope();
            var appointmentHandler =
                scope.ServiceProvider.GetRequiredService<IAppointmentHandler>();

            var nowInBr = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrTimeZone);
            var tomorrowBrDate = nowInBr.Date.AddDays(1);

            var startOfTomorrowBrInUtc = new DateTimeOffset(
                tomorrowBrDate,
                BrTimeZone.GetUtcOffset(tomorrowBrDate)
            ).ToUniversalTime();
            var endOfTomorrowBrInUtc = startOfTomorrowBrInUtc.AddDays(1).AddTicks(-1);

            var appointments = await appointmentHandler.GetAppointmentsByDateRange(
                startOfTomorrowBrInUtc,
                endOfTomorrowBrInUtc,
                ct
            );

            var toProcess = appointments
                .Where(a => a.AppointmentStatus == AppointmentStatusEnum.Marcada && !a.ReminderSent)
                .ToList();

            logger.LogInformation(
                "Found {Count} appointments to notify for tomorrow (BR Time).",
                toProcess.Count
            );

            foreach (var appointment in toProcess)
            {
                await SendAppointmentReminder(appointment, ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing email reminders");
        }
    }

    private async Task SendAppointmentReminder(Appointment appointment, CancellationToken ct)
    {
        try
        {
            if (appointment.Pacient == null || string.IsNullOrEmpty(appointment.Pacient.Email))
                return;

            var html = await _razor.CompileRenderAsync("AppointmentReminder.cshtml", appointment);

            var http = httpFactory.CreateClient("resend");
            await http.PostAsJsonAsync(
                "emails",
                new
                {
                    from = $"Isis Vitória <{Environment.GetEnvironmentVariable("RESEND_SENDER_EMAIL") ?? "onboarding@resend.dev"}>",
                    to = new[] { appointment.Pacient.Email },
                    subject = "Lembrete de Sessão - Psicoterapia",
                    html,
                },
                ct
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending reminder to {Email}", appointment.Pacient?.Email);
        }
    }
}
