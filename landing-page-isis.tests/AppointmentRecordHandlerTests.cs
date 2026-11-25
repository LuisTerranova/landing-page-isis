using landing_page_isis.core;
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
        Assert.NotNull(db.RectifiedAt);
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

    [Fact]
    public async Task SaveDraft_ShouldCreateDraft_WhenNoneExists()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentRecordHandler(context);

        var appointmentId = Guid.NewGuid();
        context.Appointments.Add(
            new Appointment
            {
                Id = appointmentId,
                PatientId = Guid.NewGuid(),
                AppointmentDate = DateTimeOffset.UtcNow,
                AppointmentStatus = AppointmentStatusEnum.Marcada,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.SaveDraft(
            appointmentId,
            "Rascunho automático.",
            CancellationToken.None
        );

        Assert.True(result.Success);
        var record = await context.AppointmentRecords.SingleAsync(r =>
            r.AppointmentId == appointmentId
        );
        Assert.Equal("Rascunho automático.", record.Note);
        Assert.True(record.IsDraft);
        Assert.NotNull(record.UpdatedAt);
    }

    [Fact]
    public async Task SaveDraft_ShouldUpdateTheSameDraft()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentRecordHandler(context);

        var appointmentId = Guid.NewGuid();
        var recordId = Guid.NewGuid();
        context.Appointments.Add(
            new Appointment
            {
                Id = appointmentId,
                PatientId = Guid.NewGuid(),
                AppointmentDate = DateTimeOffset.UtcNow,
                AppointmentStatus = AppointmentStatusEnum.Marcada,
            }
        );
        context.AppointmentRecords.Add(
            new AppointmentRecord
            {
                Id = recordId,
                AppointmentId = appointmentId,
                Note = "Rascunho antigo.",
                IsDraft = true,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.SaveDraft(
            appointmentId,
            "Rascunho atualizado.",
            CancellationToken.None
        );

        Assert.True(result.Success);
        var records = await context.AppointmentRecords.ToListAsync();
        var record = Assert.Single(records);
        Assert.Equal(recordId, record.Id);
        Assert.Equal("Rascunho atualizado.", record.Note);
        Assert.True(record.IsDraft);
        Assert.NotNull(record.UpdatedAt);
    }

    [Fact]
    public async Task SaveDraft_ShouldRemoveDraft_WhenNoteIsEmpty()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentRecordHandler(context);

        var appointmentId = Guid.NewGuid();
        context.Appointments.Add(
            new Appointment
            {
                Id = appointmentId,
                PatientId = Guid.NewGuid(),
                AppointmentDate = DateTimeOffset.UtcNow,
                AppointmentStatus = AppointmentStatusEnum.Marcada,
            }
        );
        context.AppointmentRecords.Add(
            new AppointmentRecord
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                Note = "Rascunho antigo.",
                IsDraft = true,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.SaveDraft(appointmentId, "   ", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Empty(await context.AppointmentRecords.ToListAsync());
    }

    [Fact]
    public async Task SaveDraft_ShouldNotRemoveFinalRecord_WhenNoteIsEmpty()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentRecordHandler(context);

        var appointmentId = Guid.NewGuid();
        context.Appointments.Add(
            new Appointment
            {
                Id = appointmentId,
                PatientId = Guid.NewGuid(),
                AppointmentDate = DateTimeOffset.UtcNow,
                AppointmentStatus = AppointmentStatusEnum.Marcada,
            }
        );
        context.AppointmentRecords.Add(
            new AppointmentRecord
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                Note = "Nota final.",
                IsDraft = false,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.SaveDraft(appointmentId, "", CancellationToken.None);

        Assert.True(result.Success);
        var record = await context.AppointmentRecords.SingleAsync();
        Assert.Equal("Nota final.", record.Note);
        Assert.False(record.IsDraft);
    }

    [Fact]
    public async Task SaveDraft_ShouldNotChangeCompletedAppointment()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentRecordHandler(context);

        var appointmentId = Guid.NewGuid();
        context.Appointments.Add(
            new Appointment
            {
                Id = appointmentId,
                PatientId = Guid.NewGuid(),
                AppointmentDate = DateTimeOffset.UtcNow,
                AppointmentStatus = AppointmentStatusEnum.Realizada,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.SaveDraft(
            appointmentId,
            "Rascunho após finalização.",
            CancellationToken.None
        );

        Assert.False(result.Success);
        Assert.Equal("Consulta já finalizada.", result.Message);
        Assert.Empty(await context.AppointmentRecords.ToListAsync());
    }

    [Fact]
    public async Task SaveDraft_ShouldRejectCancelledAppointment()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentRecordHandler(context);

        var appointmentId = Guid.NewGuid();
        context.Appointments.Add(
            new Appointment
            {
                Id = appointmentId,
                PatientId = Guid.NewGuid(),
                AppointmentDate = DateTimeOffset.UtcNow,
                AppointmentStatus = AppointmentStatusEnum.Cancelada,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.SaveDraft(
            appointmentId,
            "Rascunho de consulta cancelada.",
            CancellationToken.None
        );

        Assert.False(result.Success);
        Assert.Equal("Não é possível salvar rascunho de uma consulta cancelada.", result.Message);
        Assert.Empty(await context.AppointmentRecords.ToListAsync());
    }

    [Fact]
    public async Task SaveDraft_ShouldNotOverwriteFinalRecord()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentRecordHandler(context);

        var appointmentId = Guid.NewGuid();
        context.Appointments.Add(
            new Appointment
            {
                Id = appointmentId,
                PatientId = Guid.NewGuid(),
                AppointmentDate = DateTimeOffset.UtcNow,
                AppointmentStatus = AppointmentStatusEnum.Marcada,
            }
        );
        context.AppointmentRecords.Add(
            new AppointmentRecord
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                Note = "Nota final.",
                IsDraft = false,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.SaveDraft(
            appointmentId,
            "Rascunho indevido.",
            CancellationToken.None
        );

        Assert.False(result.Success);
        var record = await context.AppointmentRecords.SingleAsync();
        Assert.Equal("Nota final.", record.Note);
        Assert.False(record.IsDraft);
    }

    [Fact]
    public async Task UpdateAppointmentRecord_ShouldBlockDrafts()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentRecordHandler(context);

        var recordId = Guid.NewGuid();
        context.AppointmentRecords.Add(
            new AppointmentRecord
            {
                Id = recordId,
                AppointmentId = Guid.NewGuid(),
                Note = "Rascunho.",
                IsDraft = true,
                CreatedAt = DateTimeOffset.UtcNow,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.UpdateAppointmentRecord(
            new AppointmentRecord { Id = recordId, Note = "Retificação." }
        );

        Assert.False(result.Success);
        Assert.Contains("rascunho", result.Message);
        var record = await context.AppointmentRecords.SingleAsync();
        Assert.Equal("Rascunho.", record.Note);
    }

    [Fact]
    public async Task GetDraftByAppointmentId_ShouldReturnOnlyDrafts()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentRecordHandler(context);

        var draftAppointmentId = Guid.NewGuid();
        var finalAppointmentId = Guid.NewGuid();

        context.AppointmentRecords.AddRange(
            new AppointmentRecord
            {
                Id = Guid.NewGuid(),
                AppointmentId = draftAppointmentId,
                Note = "Rascunho.",
                IsDraft = true,
                CreatedAt = DateTimeOffset.UtcNow,
            },
            new AppointmentRecord
            {
                Id = Guid.NewGuid(),
                AppointmentId = finalAppointmentId,
                Note = "Final.",
                IsDraft = false,
                CreatedAt = DateTimeOffset.UtcNow,
            }
        );
        await context.SaveChangesAsync();

        var draft = await handler.GetDraftByAppointmentId(
            draftAppointmentId,
            CancellationToken.None
        );
        var final = await handler.GetDraftByAppointmentId(
            finalAppointmentId,
            CancellationToken.None
        );

        Assert.NotNull(draft);
        Assert.True(draft.IsDraft);
        Assert.Null(final);
    }
}
