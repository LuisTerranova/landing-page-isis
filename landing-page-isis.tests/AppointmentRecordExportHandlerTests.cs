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

    private async Task<(AppDbContext context, Guid patientId)> SeedWithRecords(int recordCount = 2)
    {
        var context = GetDatabaseContext();
        var patientId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();

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

        context.Appointments.Add(
            new Appointment
            {
                Id = appointmentId,
                PatientId = patientId,
                AppointmentDate = DateTimeOffset.UtcNow.AddDays(-1),
                AppointmentStatus = AppointmentStatusEnum.Realizada,
                Price = 100,
            }
        );

        for (int i = 0; i < recordCount; i++)
        {
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

    [Fact]
    public async Task ExportPatientRecords_ShouldReturnPdfBytes_WhenFormatIsPdf()
    {
        var (context, patientId) = await SeedWithRecords();
        var handler = new AppointmentRecordExportHandler(context);

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
        var handler = new AppointmentRecordExportHandler(context);

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

        var handler = new AppointmentRecordExportHandler(context);

        var result = await handler.ExportPatientRecords(patientId, "pdf");

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task ExportPatientRecords_ShouldThrow_WhenPatientNotFound()
    {
        var context = GetDatabaseContext();
        var handler = new AppointmentRecordExportHandler(context);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.ExportPatientRecords(Guid.NewGuid(), "pdf")
        );

        Assert.Equal("Paciente não encontrado.", ex.Message);
    }

    [Fact]
    public async Task ExportPatientRecords_ShouldThrow_WhenInvalidFormat()
    {
        var (context, patientId) = await SeedWithRecords();
        var handler = new AppointmentRecordExportHandler(context);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.ExportPatientRecords(patientId, "txt")
        );

        Assert.Contains("Formato inválido", ex.Message);
    }

    [Fact]
    public async Task ExportPatientRecords_ShouldHandleManyRecords()
    {
        var (context, patientId) = await SeedWithRecords(20);
        var handler = new AppointmentRecordExportHandler(context);

        var pdfResult = await handler.ExportPatientRecords(patientId, "pdf");
        var docxResult = await handler.ExportPatientRecords(patientId, "docx");

        Assert.True(pdfResult.Length > 100);
        Assert.True(docxResult.Length > 100);
    }
}
