using landing_page_isis.core;
using landing_page_isis.core.Models;
using landing_page_isis.Handlers;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.tests;

public class ProntuarioHandlerTests
{
    private static AppDbContext GetDatabaseContext()
    {
        Environment.SetEnvironmentVariable("ENCRYPTION_KEY", "test-encryption-key-32-bytes----");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task SeedPatient(AppDbContext context, Guid patientId)
    {
        context.Patients.Add(
            new Patient
            {
                Id = patientId,
                Name = "Paciente Teste",
                Phone = "69999999999",
            }
        );
        await context.SaveChangesAsync();
    }

    private static void AddAppointmentWithRecord(
        AppDbContext context,
        Guid patientId,
        DateTimeOffset appointmentDate,
        string note,
        bool isDraft = false,
        Guid? coupleId = null
    )
    {
        var appointmentId = Guid.NewGuid();
        context.Appointments.Add(
            new Appointment
            {
                Id = appointmentId,
                PatientId = patientId,
                CoupleId = coupleId,
                AppointmentDate = appointmentDate,
                AppointmentStatus = isDraft
                    ? AppointmentStatusEnum.Marcada
                    : AppointmentStatusEnum.Realizada,
            }
        );
        context.AppointmentRecords.Add(
            new AppointmentRecord
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                Note = note,
                IsDraft = isDraft,
                CreatedAt = appointmentDate,
            }
        );
    }

    private static void AddNote(
        AppDbContext context,
        Guid? patientId,
        Guid? coupleId,
        DateTimeOffset noteDate,
        string content,
        bool isDeleted = false
    )
    {
        context.AdditionalNotes.Add(
            new AdditionalNote
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                CoupleId = coupleId,
                NoteDate = noteDate,
                Content = content,
                CreatedAt = noteDate,
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? noteDate : null,
            }
        );
    }

    [Fact]
    public async Task GetTimelineByPatientId_ShouldMixAppointmentsAndNotes()
    {
        await using var context = GetDatabaseContext();
        var patientId = Guid.NewGuid();
        await SeedPatient(context, patientId);

        AddAppointmentWithRecord(
            context,
            patientId,
            new DateTimeOffset(2026, 3, 10, 14, 0, 0, TimeSpan.Zero),
            "Consulta A"
        );
        AddAppointmentWithRecord(
            context,
            patientId,
            new DateTimeOffset(2026, 3, 12, 14, 0, 0, TimeSpan.Zero),
            "Consulta B"
        );
        AddNote(
            context,
            patientId,
            null,
            new DateTimeOffset(2026, 3, 11, 10, 0, 0, TimeSpan.Zero),
            "Nota avulsa"
        );
        await context.SaveChangesAsync();

        var handler = new ProntuarioHandler(context);
        var result = await handler.GetTimelineByPatientId(
            0,
            10,
            patientId,
            null,
            CancellationToken.None
        );

        var items = result.Items.ToList();

        Assert.Equal(3, result.TotalItems);
        Assert.Equal(ProntuarioItemType.Consulta, items[0].Type);
        Assert.Equal("Consulta B", items[0].Content);
        Assert.Equal(ProntuarioItemType.Anotacao, items[1].Type);
        Assert.Equal("Nota avulsa", items[1].Content);
        Assert.Equal(ProntuarioItemType.Consulta, items[2].Type);
        Assert.Equal("Consulta A", items[2].Content);
        Assert.Null(items[1].AppointmentId);
    }

    [Fact]
    public async Task GetTimelineByPatientId_ShouldOrderByEventDateThenCreatedAt()
    {
        await using var context = GetDatabaseContext();
        var patientId = Guid.NewGuid();
        await SeedPatient(context, patientId);

        var sameDate = new DateTimeOffset(2026, 5, 20, 9, 0, 0, TimeSpan.Zero);

        context.AdditionalNotes.AddRange(
            new AdditionalNote
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                NoteDate = sameDate,
                Content = "Nota mais antiga",
                CreatedAt = sameDate.AddHours(-2),
            },
            new AdditionalNote
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                NoteDate = sameDate,
                Content = "Nota mais nova",
                CreatedAt = sameDate,
            }
        );
        await context.SaveChangesAsync();

        var handler = new ProntuarioHandler(context);
        var result = await handler.GetTimelineByPatientId(
            0,
            10,
            patientId,
            null,
            CancellationToken.None
        );

        var items = result.Items.ToList();

        Assert.Equal(2, items.Count);
        Assert.Equal("Nota mais nova", items[0].Content);
        Assert.Equal("Nota mais antiga", items[1].Content);
    }

    [Fact]
    public async Task GetTimelineByPatientId_ShouldPaginateTheUnion()
    {
        await using var context = GetDatabaseContext();
        var patientId = Guid.NewGuid();
        await SeedPatient(context, patientId);

        for (var i = 0; i < 5; i++)
        {
            AddNote(
                context,
                patientId,
                null,
                new DateTimeOffset(2026, 6, 1 + i, 10, 0, 0, TimeSpan.Zero),
                $"Nota {i}"
            );
        }
        await context.SaveChangesAsync();

        var handler = new ProntuarioHandler(context);
        var result = await handler.GetTimelineByPatientId(
            1,
            2,
            patientId,
            null,
            CancellationToken.None
        );

        Assert.Equal(5, result.TotalItems);
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal("Nota 2", result.Items.First().Content);
    }

    [Fact]
    public async Task GetTimelineByPatientId_ShouldFilterByMonthUsingNoteDate()
    {
        await using var context = GetDatabaseContext();
        var patientId = Guid.NewGuid();
        await SeedPatient(context, patientId);

        AddNote(
            context,
            patientId,
            null,
            new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero),
            "Nota de março"
        );
        AddNote(
            context,
            patientId,
            null,
            new DateTimeOffset(2026, 4, 15, 10, 0, 0, TimeSpan.Zero),
            "Nota de abril"
        );
        await context.SaveChangesAsync();

        var handler = new ProntuarioHandler(context);
        var result = await handler.GetTimelineByPatientId(
            0,
            10,
            patientId,
            new DateTime(2026, 3, 1),
            CancellationToken.None
        );

        var item = Assert.Single(result.Items);
        Assert.Equal("Nota de março", item.Content);
    }

    [Fact]
    public async Task GetTimelineByCoupleId_ShouldIncludeBothPartnersAndCoupleAppointments()
    {
        await using var context = GetDatabaseContext();

        var partner1 = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Parceiro 1",
            Phone = "69999999999",
        };
        var partner2 = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Parceiro 2",
            Phone = "69999999998",
        };
        var other = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Outro",
            Phone = "69999999997",
        };
        var coupleId = Guid.NewGuid();

        context.Patients.AddRange(partner1, partner2, other);
        context.Couples.Add(
            new Couple
            {
                Id = coupleId,
                Name = "Casal Teste",
                Patient1Id = partner1.Id,
                Patient2Id = partner2.Id,
            }
        );
        await context.SaveChangesAsync();

        AddAppointmentWithRecord(
            context,
            partner1.Id,
            new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.Zero),
            "Consulta parceiro 1"
        );
        AddAppointmentWithRecord(
            context,
            partner2.Id,
            new DateTimeOffset(2026, 7, 2, 10, 0, 0, TimeSpan.Zero),
            "Consulta parceiro 2"
        );
        AddAppointmentWithRecord(
            context,
            partner1.Id,
            new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero),
            "Consulta do casal",
            coupleId: coupleId
        );
        AddAppointmentWithRecord(
            context,
            other.Id,
            new DateTimeOffset(2026, 7, 4, 10, 0, 0, TimeSpan.Zero),
            "Consulta de outro paciente"
        );
        AddNote(
            context,
            null,
            coupleId,
            new DateTimeOffset(2026, 7, 5, 10, 0, 0, TimeSpan.Zero),
            "Nota do casal"
        );
        AddNote(
            context,
            partner1.Id,
            null,
            new DateTimeOffset(2026, 7, 6, 10, 0, 0, TimeSpan.Zero),
            "Nota individual"
        );
        await context.SaveChangesAsync();

        var handler = new ProntuarioHandler(context);
        var result = await handler.GetTimelineByCoupleId(
            0,
            10,
            coupleId,
            null,
            CancellationToken.None
        );

        var items = result.Items.ToList();

        Assert.Equal(4, result.TotalItems);
        Assert.Equal(4, items.Select(i => i.Id).Distinct().Count());
        Assert.Contains(items, i => i.Content == "Consulta parceiro 1");
        Assert.Contains(items, i => i.Content == "Consulta parceiro 2");
        Assert.Contains(items, i => i.Content == "Consulta do casal");
        Assert.Contains(items, i => i.Content == "Nota do casal");
        Assert.DoesNotContain(items, i => i.Content == "Consulta de outro paciente");
        Assert.DoesNotContain(items, i => i.Content == "Nota individual");
    }

    [Fact]
    public async Task GetTimelineByCoupleId_ShouldReturnEmpty_WhenCoupleDoesNotExist()
    {
        await using var context = GetDatabaseContext();
        var handler = new ProntuarioHandler(context);

        var result = await handler.GetTimelineByCoupleId(
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
    public async Task GetTimelineByPatientId_ShouldNotIncludeDeletedNotes()
    {
        await using var context = GetDatabaseContext();
        var patientId = Guid.NewGuid();
        await SeedPatient(context, patientId);

        AddNote(
            context,
            patientId,
            null,
            new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero),
            "Nota ativa"
        );
        AddNote(
            context,
            patientId,
            null,
            new DateTimeOffset(2026, 8, 2, 10, 0, 0, TimeSpan.Zero),
            "Nota deletada",
            isDeleted: true
        );
        await context.SaveChangesAsync();

        var handler = new ProntuarioHandler(context);
        var result = await handler.GetTimelineByPatientId(
            0,
            10,
            patientId,
            null,
            CancellationToken.None
        );

        var item = Assert.Single(result.Items);
        Assert.Equal("Nota ativa", item.Content);
    }

    [Fact]
    public async Task GetTimelineByPatientId_ShouldIncludeDrafts()
    {
        await using var context = GetDatabaseContext();
        var patientId = Guid.NewGuid();
        await SeedPatient(context, patientId);

        AddAppointmentWithRecord(
            context,
            patientId,
            new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero),
            "Rascunho automático",
            isDraft: true
        );
        await context.SaveChangesAsync();

        var handler = new ProntuarioHandler(context);
        var result = await handler.GetTimelineByPatientId(
            0,
            10,
            patientId,
            null,
            CancellationToken.None
        );

        var item = Assert.Single(result.Items);
        Assert.True(item.IsDraft);
        Assert.Equal(ProntuarioItemType.Consulta, item.Type);
    }

    [Fact]
    public async Task GetFullTimelineByPatientId_ShouldExcludeDrafts()
    {
        await using var context = GetDatabaseContext();
        var patientId = Guid.NewGuid();
        await SeedPatient(context, patientId);

        AddAppointmentWithRecord(
            context,
            patientId,
            new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero),
            "Nota final",
            isDraft: false
        );
        AddAppointmentWithRecord(
            context,
            patientId,
            new DateTimeOffset(2026, 10, 2, 10, 0, 0, TimeSpan.Zero),
            "Rascunho",
            isDraft: true
        );
        await context.SaveChangesAsync();

        var handler = new ProntuarioHandler(context);
        var result = await handler.GetFullTimelineByPatientId(
            patientId,
            includeNotes: true,
            includeDrafts: false,
            CancellationToken.None
        );

        var item = Assert.Single(result);
        Assert.Equal("Nota final", item.Content);
        Assert.False(item.IsDraft);
    }

    [Fact]
    public async Task GetFullTimelineByPatientId_ShouldRespectIncludeNotes()
    {
        await using var context = GetDatabaseContext();
        var patientId = Guid.NewGuid();
        await SeedPatient(context, patientId);

        AddAppointmentWithRecord(
            context,
            patientId,
            new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero),
            "Consulta"
        );
        AddNote(
            context,
            patientId,
            null,
            new DateTimeOffset(2026, 11, 2, 10, 0, 0, TimeSpan.Zero),
            "Nota avulsa"
        );
        await context.SaveChangesAsync();

        var handler = new ProntuarioHandler(context);

        var withoutNotes = await handler.GetFullTimelineByPatientId(
            patientId,
            includeNotes: false,
            includeDrafts: false,
            CancellationToken.None
        );
        var withNotes = await handler.GetFullTimelineByPatientId(
            patientId,
            includeNotes: true,
            includeDrafts: false,
            CancellationToken.None
        );

        Assert.Single(withoutNotes);
        Assert.Equal("Consulta", withoutNotes[0].Content);
        Assert.Equal(2, withNotes.Count);
    }

    [Fact]
    public async Task GetFullTimelineByCoupleId_ShouldExcludeDraftsAndDeletedNotes()
    {
        await using var context = GetDatabaseContext();

        var partner1 = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Parceiro 1",
            Phone = "69999999999",
        };
        var partner2 = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Parceiro 2",
            Phone = "69999999998",
        };
        var coupleId = Guid.NewGuid();

        context.Patients.AddRange(partner1, partner2);
        context.Couples.Add(
            new Couple
            {
                Id = coupleId,
                Name = "Casal Teste",
                Patient1Id = partner1.Id,
                Patient2Id = partner2.Id,
            }
        );
        await context.SaveChangesAsync();

        AddAppointmentWithRecord(
            context,
            partner1.Id,
            new DateTimeOffset(2026, 12, 1, 10, 0, 0, TimeSpan.Zero),
            "Consulta final"
        );
        AddAppointmentWithRecord(
            context,
            partner2.Id,
            new DateTimeOffset(2026, 12, 2, 10, 0, 0, TimeSpan.Zero),
            "Rascunho",
            isDraft: true
        );
        AddNote(
            context,
            null,
            coupleId,
            new DateTimeOffset(2026, 12, 3, 10, 0, 0, TimeSpan.Zero),
            "Nota do casal"
        );
        AddNote(
            context,
            null,
            coupleId,
            new DateTimeOffset(2026, 12, 4, 10, 0, 0, TimeSpan.Zero),
            "Nota deletada",
            isDeleted: true
        );
        await context.SaveChangesAsync();

        var handler = new ProntuarioHandler(context);
        var result = await handler.GetFullTimelineByCoupleId(
            coupleId,
            includeNotes: true,
            includeDrafts: false,
            CancellationToken.None
        );

        Assert.Equal(2, result.Count);
        Assert.Contains(result, i => i.Content == "Consulta final");
        Assert.Contains(result, i => i.Content == "Nota do casal");
    }
}
