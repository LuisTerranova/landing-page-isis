using DocumentFormat.OpenXml.Packaging;
using landing_page_isis.core;
using landing_page_isis.core.Models;
using landing_page_isis.Handlers;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.tests;

public class AppointmentRecordExportHandlerTests
{
    static AppointmentRecordExportHandlerTests()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

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

    private static AppointmentRecordExportHandler GetHandler(AppDbContext context)
    {
        return new AppointmentRecordExportHandler(context, new ProntuarioHandler(context));
    }

    private static string ReadDocxText(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        return doc.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty;
    }

    private static async Task<(AppDbContext context, Guid patientId)> SeedWithRecords(
        int recordCount = 2
    )
    {
        var context = GetDatabaseContext();
        var patientId = Guid.NewGuid();

        context.Patients.Add(
            new Patient
            {
                Id = patientId,
                Name = "Maria Silva",
                Phone = "69999999999",
                Email = "maria@test.com",
                Cpf = "12345678901",
            }
        );

        for (int i = 0; i < recordCount; i++)
        {
            var appointmentId = Guid.NewGuid();

            context.Appointments.Add(
                new Appointment
                {
                    Id = appointmentId,
                    PatientId = patientId,
                    AppointmentDate = DateTimeOffset.UtcNow.AddDays(-recordCount + i),
                    AppointmentStatus = AppointmentStatusEnum.Realizada,
                    Price = 100,
                }
            );

            context.AppointmentRecords.Add(
                new AppointmentRecord
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    Note = $"Nota da sessão {i + 1}.",
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-recordCount + i),
                }
            );
        }

        await context.SaveChangesAsync();
        return (context, patientId);
    }

    private static async Task AddAppointmentRecord(
        AppDbContext context,
        Guid patientId,
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
                AppointmentDate = DateTimeOffset.UtcNow.AddDays(-1),
                AppointmentStatus = isDraft
                    ? AppointmentStatusEnum.Marcada
                    : AppointmentStatusEnum.Realizada,
                Price = 100,
            }
        );
        context.AppointmentRecords.Add(
            new AppointmentRecord
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                Note = note,
                IsDraft = isDraft,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            }
        );
        await context.SaveChangesAsync();
    }

    private static async Task AddNote(
        AppDbContext context,
        Guid? patientId,
        Guid? coupleId,
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
                Content = content,
                NoteDate = DateTimeOffset.UtcNow.AddDays(-1),
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            }
        );
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task ExportPatientRecords_ShouldReturnPdfBytes_WhenFormatIsPdf()
    {
        var (context, patientId) = await SeedWithRecords();
        var handler = GetHandler(context);

        var result = await handler.ExportPatientRecords(patientId, "pdf");

        Assert.NotNull(result);
        Assert.True(result.Length > 0, "PDF deve conter dados");
        // PDF magic bytes: %PDF
        Assert.Equal(0x25, result[0]);
        Assert.Equal(0x50, result[1]);
        Assert.Equal(0x44, result[2]);
        Assert.Equal(0x46, result[3]);
    }

    [Fact]
    public async Task ExportPatientRecords_ShouldReturnDocxBytes_WhenFormatIsDocx()
    {
        var (context, patientId) = await SeedWithRecords();
        var handler = GetHandler(context);

        var result = await handler.ExportPatientRecords(patientId, "docx");

        Assert.NotNull(result);
        Assert.True(result.Length > 0, "DOCX deve conter dados");
        // DOCX magic bytes: PK (ZIP)
        Assert.Equal(0x50, result[0]);
        Assert.Equal(0x4B, result[1]);
    }

    [Fact]
    public async Task ExportPatientRecords_ShouldReturnEmptyRecordsMessage_WhenNoRecords()
    {
        var context = GetDatabaseContext();
        var patientId = Guid.NewGuid();

        context.Patients.Add(
            new Patient
            {
                Id = patientId,
                Name = "João",
                Phone = "69988888888",
            }
        );
        await context.SaveChangesAsync();

        var handler = GetHandler(context);

        var result = await handler.ExportPatientRecords(patientId, "pdf");

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task ExportPatientRecords_ShouldThrow_WhenPatientNotFound()
    {
        var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.ExportPatientRecords(Guid.NewGuid(), "pdf")
        );

        Assert.Equal("Paciente não encontrado.", ex.Message);
    }

    [Fact]
    public async Task ExportPatientRecords_ShouldThrow_WhenInvalidFormat()
    {
        var (context, patientId) = await SeedWithRecords();
        var handler = GetHandler(context);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.ExportPatientRecords(patientId, "txt")
        );

        Assert.Contains("Formato inválido", ex.Message);
    }

    [Fact]
    public async Task ExportPatientRecords_ShouldNotIncludeNotes_WhenIncludeNotesIsFalse()
    {
        var (context, patientId) = await SeedWithRecords();
        await AddNote(context, patientId, null, "Nota avulsa confidencial");
        var handler = GetHandler(context);

        var result = await handler.ExportPatientRecords(patientId, "docx", includeNotes: false);
        var text = ReadDocxText(result);

        Assert.Contains("Consulta", text);
        Assert.DoesNotContain("Nota avulsa confidencial", text);
    }

    [Fact]
    public async Task ExportPatientRecords_ShouldIncludeNotes_WhenIncludeNotesIsTrue()
    {
        var (context, patientId) = await SeedWithRecords();
        await AddNote(context, patientId, null, "Nota avulsa confidencial");
        var handler = GetHandler(context);

        var result = await handler.ExportPatientRecords(patientId, "docx", includeNotes: true);
        var text = ReadDocxText(result);

        Assert.Contains("Nota Avulsa", text);
        Assert.Contains("Nota avulsa confidencial", text);
    }

    [Fact]
    public async Task ExportPatientRecords_ShouldNotIncludeDrafts()
    {
        var (context, patientId) = await SeedWithRecords();
        await AddAppointmentRecord(context, patientId, "Rascunho confidencial", isDraft: true);
        var handler = GetHandler(context);

        var result = await handler.ExportPatientRecords(patientId, "docx", includeNotes: true);
        var text = ReadDocxText(result);

        Assert.DoesNotContain("Rascunho confidencial", text);
        Assert.Contains("Nota da sessão 1.", text);
    }

    [Fact]
    public async Task ExportPatientRecords_ShouldNotIncludeDeletedNotes()
    {
        var (context, patientId) = await SeedWithRecords();
        await AddNote(context, patientId, null, "Nota removida logicamente", isDeleted: true);
        var handler = GetHandler(context);

        var result = await handler.ExportPatientRecords(patientId, "docx", includeNotes: true);
        var text = ReadDocxText(result);

        Assert.DoesNotContain("Nota removida logicamente", text);
    }

    [Fact]
    public async Task ExportCoupleRecords_ShouldGeneratePdfAndDocx_WithBothPartners()
    {
        var context = GetDatabaseContext();

        var partner1 = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Ana Souza",
            Phone = "69999999999",
        };
        var partner2 = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Bruno Souza",
            Phone = "69999999998",
        };
        var coupleId = Guid.NewGuid();

        context.Patients.AddRange(partner1, partner2);
        context.Couples.Add(
            new Couple
            {
                Id = coupleId,
                Name = "Ana e Bruno",
                Patient1Id = partner1.Id,
                Patient2Id = partner2.Id,
            }
        );
        await context.SaveChangesAsync();

        await AddAppointmentRecord(context, partner1.Id, "Consulta da Ana");
        await AddAppointmentRecord(context, partner2.Id, "Consulta do Bruno");
        await AddAppointmentRecord(context, partner1.Id, "Consulta do casal", coupleId: coupleId);
        await AddNote(context, null, coupleId, "Nota do casal");

        var handler = GetHandler(context);

        var pdf = await handler.ExportCoupleRecords(coupleId, "pdf", includeNotes: true);
        Assert.Equal(0x25, pdf[0]);
        Assert.Equal(0x50, pdf[1]);
        Assert.Equal(0x44, pdf[2]);
        Assert.Equal(0x46, pdf[3]);

        var docx = await handler.ExportCoupleRecords(coupleId, "docx", includeNotes: true);
        var text = ReadDocxText(docx);

        Assert.Contains("PRONTUÁRIO DO CASAL", text);
        Assert.Contains("Ana e Bruno", text);
        Assert.Contains("Ana Souza", text);
        Assert.Contains("Bruno Souza", text);
        Assert.Contains("Consulta da Ana", text);
        Assert.Contains("Consulta do Bruno", text);
        Assert.Contains("Consulta do casal", text);
        Assert.Contains("Nota do casal", text);
    }

    [Fact]
    public async Task ExportCoupleRecords_ShouldThrow_WhenCoupleNotFound()
    {
        var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.ExportCoupleRecords(Guid.NewGuid(), "pdf")
        );

        Assert.Equal("Casal não encontrado.", ex.Message);
    }

    [Fact]
    public async Task ExportPatientRecords_ShouldHandleManyRecords()
    {
        var (context, patientId) = await SeedWithRecords(20);
        var handler = GetHandler(context);

        var pdfResult = await handler.ExportPatientRecords(patientId, "pdf");
        var docxResult = await handler.ExportPatientRecords(patientId, "docx");

        Assert.True(pdfResult.Length > 100);
        Assert.True(docxResult.Length > 100);
    }
}
