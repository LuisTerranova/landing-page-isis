using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;

namespace landing_page_isis.Services;

public class EmailService(IServiceProvider services) : BackgroundService
{
    private static readonly TimeZoneInfo BrTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
        "America/Sao_Paulo"
    );
    private readonly TimeSpan _period = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
            var localSender = scope.ServiceProvider.GetRequiredService<ISender>();

            // Calculate tomorrow in Brasilia time
            var nowInBr = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrTimeZone);
            var tomorrowBrDate = nowInBr.Date.AddDays(1);

            // Map the BR date boundaries to UTC offsets for database query
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

            foreach (
                var appointment in appointments.Where(a =>
                    a.AppointmentStatus == AppointmentStatusEnum.Marcada
                )
            )
            {
                await SendAppointmentReminder(appointment, localSender, ct);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing email reminders: {ex.Message}");
        }
    }

    private async Task SendAppointmentReminder(
        Appointment appointment,
        ISender localSender,
        CancellationToken ct
    )
    {
        try
        {
            if (appointment.Pacient == null || string.IsNullOrEmpty(appointment.Pacient.Email))
                return;

            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Templates",
                "AppointmentReminder.cshtml"
            );

            var email = Email
                .From(
                    Environment.GetEnvironmentVariable("RESEND_SENDER_EMAIL")
                        ?? "onboarding@resend.dev",
                    "Isis Vitória"
                )
                .To(appointment.Pacient.Email)
                .Subject("Lembrete de Sessão - Psicoterapia")
                .UsingTemplateFromFile(templatePath, appointment);

            email.Sender = localSender;
            await email.SendAsync(ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error sending reminder to {appointment.Pacient?.Email}: {ex.Message}"
            );
        }
    }
}
