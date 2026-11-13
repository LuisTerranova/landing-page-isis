using landing_page_isis.core.Validators;
using landing_page_isis.Handlers;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.tests;

public class AdditionalNoteHandlerTests
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

    private static AdditionalNoteHandler GetHandler(AppDbContext context)
    {
        return new AdditionalNoteHandler(context, new AdditionalNoteValidator());
    }

    private static async Task<Guid> SeedPatient(AppDbContext context)
    {
        var patientId = Guid.NewGuid();
        context.Patients.Add(
            new landing_page_isis.core.Models.Patient
            {
                Id = patientId,
                Name = "Paciente Teste",
                Phone = "69999999999",
            }
        );
        await context.SaveChangesAsync();
        return patientId;
    }

    private static async Task<Guid> SeedCouple(AppDbContext context)
    {
        var patient1 = new landing_page_isis.core.Models.Patient
        {
            Id = Guid.NewGuid(),
            Name = "Parceiro 1",
            Phone = "69999999999",
        };
        var patient2 = new landing_page_isis.core.Models.Patient
        {
            Id = Guid.NewGuid(),
            Name = "Parceiro 2",
            Phone = "69999999998",
        };

        var coupleId = Guid.NewGuid();
        context.Patients.AddRange(patient1, patient2);
        context.Couples.Add(
            new landing_page_isis.core.Models.Couple
            {
                Id = coupleId,
                Name = "Casal Teste",
                Patient1Id = patient1.Id,
                Patient2Id = patient2.Id,
            }
        );
        await context.SaveChangesAsync();
        return coupleId;
    }

    [Fact]
    public async Task Create_ShouldCreatePatientNote_WhenValid()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);
        var patientId = await SeedPatient(context);
        var noteDate = DateTimeOffset.UtcNow.AddDays(-1);

        var result = await handler.Create(
            patientId,
            null,
            "Nota avulsa do paciente.",
            noteDate,
            CancellationToken.None
        );

        Assert.True(result.Success);
        var note = await context.AdditionalNotes.SingleAsync();
        Assert.Equal(patientId, note.PatientId);
        Assert.Null(note.CoupleId);
        Assert.Equal("Nota avulsa do paciente.", note.Content);
        Assert.Equal(noteDate, note.NoteDate);
        Assert.False(note.IsDeleted);
    }

    [Fact]
    public async Task Create_ShouldCreateCoupleNote_WhenValid()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);
        var coupleId = await SeedCouple(context);

        var result = await handler.Create(
            null,
            coupleId,
            "Nota avulsa do casal.",
            DateTimeOffset.UtcNow,
            CancellationToken.None
        );

        Assert.True(result.Success);
        var note = await context.AdditionalNotes.SingleAsync();
        Assert.Null(note.PatientId);
        Assert.Equal(coupleId, note.CoupleId);
    }

    [Fact]
    public async Task Create_ShouldReject_WhenNoOwnerIsProvided()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var result = await handler.Create(
            null,
            null,
            "Conteúdo",
            DateTimeOffset.UtcNow,
            CancellationToken.None
        );

        Assert.False(result.Success);
        Assert.Equal(
            "A nota deve estar vinculada a exatamente um paciente ou casal.",
            result.Message
        );
        Assert.Empty(context.AdditionalNotes);
    }

    [Fact]
    public async Task Create_ShouldReject_WhenBothOwnersAreProvided()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var result = await handler.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Conteúdo",
            DateTimeOffset.UtcNow,
            CancellationToken.None
        );

        Assert.False(result.Success);
        Assert.Equal(
            "A nota deve estar vinculada a exatamente um paciente ou casal.",
            result.Message
        );
        Assert.Empty(context.AdditionalNotes);
    }

    [Fact]
    public async Task Create_ShouldReject_WhenContentIsEmpty()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);
        var patientId = await SeedPatient(context);

        var result = await handler.Create(
            patientId,
            null,
            "   ",
            DateTimeOffset.UtcNow,
            CancellationToken.None
        );

        Assert.False(result.Success);
        Assert.Equal("Conteúdo da nota é obrigatório.", result.Message);
        Assert.Empty(context.AdditionalNotes);
    }

    [Fact]
    public async Task Create_ShouldReject_WhenNoteDateIsDefault()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);
        var patientId = await SeedPatient(context);

        var result = await handler.Create(
            patientId,
            null,
            "Conteúdo",
            default,
            CancellationToken.None
        );

        Assert.False(result.Success);
        Assert.Equal("Data da nota é obrigatória.", result.Message);
    }

    [Fact]
    public async Task Update_ShouldChangeContentAndNoteDate()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);
        var patientId = await SeedPatient(context);

        await handler.Create(
            patientId,
            null,
            "Conteúdo original",
            DateTimeOffset.UtcNow.AddDays(-2),
            CancellationToken.None
        );

        var note = await context.AdditionalNotes.SingleAsync();
        var updatedDate = DateTimeOffset.UtcNow.AddDays(-1);

        var result = await handler.Update(
            note.Id,
            "Conteúdo atualizado",
            updatedDate,
            CancellationToken.None
        );

        Assert.True(result.Success);
        var updated = await context.AdditionalNotes.SingleAsync();
        Assert.Equal("Conteúdo atualizado", updated.Content);
        Assert.Equal(updatedDate, updated.NoteDate);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task Update_ShouldReturnFalse_WhenNoteDoesNotExist()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var result = await handler.Update(
            Guid.NewGuid(),
            "Conteúdo",
            DateTimeOffset.UtcNow,
            CancellationToken.None
        );

        Assert.False(result.Success);
        Assert.Equal("Nota não encontrada.", result.Message);
    }

    [Fact]
    public async Task Delete_ShouldPerformLogicalDeletion()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);
        var patientId = await SeedPatient(context);

        await handler.Create(
            patientId,
            null,
            "Conteúdo",
            DateTimeOffset.UtcNow,
            CancellationToken.None
        );

        var note = await context.AdditionalNotes.SingleAsync();

        var result = await handler.Delete(note.Id, CancellationToken.None);

        Assert.True(result.Success);
        var deleted = await context.AdditionalNotes.SingleAsync();
        Assert.True(deleted.IsDeleted);
        Assert.NotNull(deleted.DeletedAt);
    }

    [Fact]
    public async Task GetById_ShouldIgnoreLogicallyDeletedNotes()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);
        var patientId = await SeedPatient(context);

        await handler.Create(
            patientId,
            null,
            "Conteúdo",
            DateTimeOffset.UtcNow,
            CancellationToken.None
        );

        var note = await context.AdditionalNotes.SingleAsync();
        await handler.Delete(note.Id, CancellationToken.None);

        var result = await handler.GetById(note.Id, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetById_ShouldReturnNote_WhenItExistsAndIsNotDeleted()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);
        var patientId = await SeedPatient(context);

        await handler.Create(
            patientId,
            null,
            "Conteúdo",
            DateTimeOffset.UtcNow,
            CancellationToken.None
        );

        var note = await context.AdditionalNotes.SingleAsync();

        var result = await handler.GetById(note.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(note.Id, result.Id);
        Assert.Equal(patientId, result.PatientId);
        Assert.Equal("Conteúdo", result.Content);
    }
}
