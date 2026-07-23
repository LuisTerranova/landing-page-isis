using landing_page_isis.core.Models;
using landing_page_isis.Handlers;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.tests;

public class AppointmentRecordHandlerTests
{
    private AppDbContext GetDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        Environment.SetEnvironmentVariable("ENCRYPTION_KEY", "test-encryption-key-32-bytes----");
        return context;
    }

    [Fact]
    public async Task CreateAppointmentRecord_ShouldReturnTrue_WhenValid()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentRecordHandler(context);

        var record = new AppointmentRecord
        {
            Id = Guid.NewGuid(),
            AppointmentId = Guid.NewGuid(),
            Note = "Paciente apresentou melhora significativa.",
        };

        var result = await handler.CreateAppointmentRecord(record);

        Assert.True(result.Success);
        var saved = await context.AppointmentRecords.FindAsync(record.Id);
        Assert.NotNull(saved);
        Assert.Equal("Paciente apresentou melhora significativa.", saved.Note);
    }

    [Fact]
    public async Task CreateAppointmentRecord_ShouldReturnFalse_WhenNoteIsEmpty()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentRecordHandler(context);

        var record = new AppointmentRecord
        {
            Id = Guid.NewGuid(),
            AppointmentId = Guid.NewGuid(),
            Note = "",
        };

        var result = await handler.CreateAppointmentRecord(record);

        Assert.False(result.Success);
        Assert.Equal("Nota de consulta não pode estar nula.", result.Message);
    }

    [Fact]
    public async Task UpdateAppointmentRecord_ShouldReturnTrue_WhenValid()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentRecordHandler(context);

        var id = Guid.NewGuid();
        context.AppointmentRecords.Add(
            new AppointmentRecord
            {
                Id = id,
                AppointmentId = Guid.NewGuid(),
                Note = "Original note.",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            }
        );
        await context.SaveChangesAsync();

        var updated = new AppointmentRecord
        {
            Id = id,
            Note = "Retificação: paciente relata melhora.",
        };

        var result = await handler.UpdateAppointmentRecord(updated);

        Assert.True(result.Success);
        var db = await context.AppointmentRecords.FindAsync(id);
        Assert.NotNull(db);
        Assert.Contains("Original note.", db.Note);
        Assert.Contains("Retificação", db.Note);
    }

    [Fact]
    public async Task UpdateAppointmentRecord_ShouldReturnFalse_WhenNotFound()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentRecordHandler(context);

        var result = await handler.UpdateAppointmentRecord(
            new AppointmentRecord { Id = Guid.NewGuid() }
        );

        Assert.False(result.Success);
        Assert.Equal("Nota não encontrada.", result.Message);
    }

    [Fact]
    public async Task GetAppointmentRecordById_ShouldReturnRecord_WhenExists()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentRecordHandler(context);

        var id = Guid.NewGuid();
        context.AppointmentRecords.Add(
            new AppointmentRecord
            {
                Id = id,
                AppointmentId = Guid.NewGuid(),
                Note = "Paciente estável.",
                CreatedAt = DateTimeOffset.UtcNow,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.GetAppointmentRecordById(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Paciente estável.", result.Note);
    }

    [Fact]
    public async Task GetAppointmentRecordById_ShouldReturnNull_WhenNotFound()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentRecordHandler(context);

        var result = await handler.GetAppointmentRecordById(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRecordsByPatientId_ShouldReturnPaginatedRecords()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentRecordHandler(context);

        var patientId = Guid.NewGuid();
        for (int i = 0; i < 6; i++)
        {
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                AppointmentDate = DateTimeOffset.UtcNow.AddDays(-i),
                PatientId = patientId,
            };
            context.Appointments.Add(appointment);

            context.AppointmentRecords.Add(
                new AppointmentRecord
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointment.Id,
                    Note = $"Record {i}",
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-i),
                }
            );
        }
        await context.SaveChangesAsync();

        var result = await handler.GetRecordsByPatientId(
            0,
            4,
            patientId,
            null,
            CancellationToken.None
        );

        Assert.Equal(6, result.TotalItems);
        Assert.Equal(4, result.Items.Count());
    }

    [Fact]
    public async Task GetRecordsByPatientId_ShouldReturnEmpty_WhenNoPatient()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentRecordHandler(context);

        var result = await handler.GetRecordsByPatientId(
            0,
            10,
            Guid.NewGuid(),
            null,
            CancellationToken.None
        );

        Assert.Equal(0, result.TotalItems);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetRecordsByPatientId_ShouldFilterByMonth()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentRecordHandler(context);

        var patientId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Appointment in current month
        var app1 = new Appointment
        {
            Id = Guid.NewGuid(),
            AppointmentDate = now,
            PatientId = patientId,
        };
        // Appointment in next month
        var app2 = new Appointment
        {
            Id = Guid.NewGuid(),
            AppointmentDate = now.AddMonths(1),
            PatientId = patientId,
        };

        context.Appointments.AddRange(app1, app2);
        context.AppointmentRecords.AddRange(
            new AppointmentRecord
            {
                Id = Guid.NewGuid(),
                AppointmentId = app1.Id,
                Note = "Current month",
                CreatedAt = now,
            },
            new AppointmentRecord
            {
                Id = Guid.NewGuid(),
                AppointmentId = app2.Id,
                Note = "Next month",
                CreatedAt = now.AddMonths(1),
            }
        );
        await context.SaveChangesAsync();

        var filter = new DateTime(now.Year, now.Month, 1);
        var result = await handler.GetRecordsByPatientId(
            0,
            10,
            patientId,
            filter,
            CancellationToken.None
        );

        Assert.Equal(1, result.TotalItems);
        Assert.Equal("Current month", result.Items.First().Note);
    }

    [Fact]
    public async Task GetAppointmentRecordByAppointmentId_ShouldReturnRecord_WhenExists()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentRecordHandler(context);

        var appId = Guid.NewGuid();
        var recordId = Guid.NewGuid();
        context.AppointmentRecords.Add(
            new AppointmentRecord
            {
                Id = recordId,
                AppointmentId = appId,
                Note = "Sessão tranquila.",
                CreatedAt = DateTimeOffset.UtcNow,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.GetAppointmentRecordByAppointmentId(appId);

        Assert.NotNull(result);
        Assert.Equal(recordId, result.Id);
        Assert.Equal("Sessão tranquila.", result.Note);
    }
}
