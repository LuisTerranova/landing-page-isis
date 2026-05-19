using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.Extensions;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using DocumentFormat.OpenXml;
using OpenXml = DocumentFormat.OpenXml.Wordprocessing;
using PackOpenXml = DocumentFormat.OpenXml.Packaging;

namespace landing_page_isis.Handlers;

public class AppointmentRecordExportHandler(AppDbContext context) : IAppointmentRecordExportHandler
{
    private static readonly string SystemGreen = "#2E7D32";
    private static readonly string SystemBurgundy = "#800020";

    public async Task<byte[]> ExportPatientRecords(Guid patientId, string format)
    {
        var patient = await context
            .Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId);

        if (patient is null)
            throw new KeyNotFoundException("Paciente não encontrado.");

        var records = await context
            .AppointmentRecords
            .Include(ar => ar.Appointment)
            .AsNoTracking()
            .Where(ar => ar.Appointment != null && ar.Appointment.PatientId == patientId)
            .OrderByDescending(ar => ar.CreatedAt)
            .ToListAsync();

        return format.ToLowerInvariant() switch
        {
            "pdf" => GeneratePdf(patient, records),
            "docx" => GenerateDocx(patient, records),
            _ => throw new ArgumentException("Formato inválido. Use 'pdf' ou 'docx'.")
        };
    }

    private byte[] GeneratePdf(Patient patient, List<AppointmentRecord> records)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                page.Header().Element(c => ComposePdfHeader(c, patient));
                page.Content().Element(c => ComposePdfContent(c, patient, records));
                page.Footer().Element(ComposePdfFooter);
            });
        }).GeneratePdf();
    }

    private void ComposePdfHeader(IContainer container, Patient patient)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("PRONTUÁRIO DO PACIENTE")
                        .FontSize(20).Bold().FontColor(Color.FromHex(SystemGreen));

                    c.Item().Text($"Gerado em: {DateTimeOffset.UtcNow.ToPortoVelhoTime():dd/MM/yyyy HH:mm}")
                        .FontSize(10).FontColor(Colors.Grey.Medium);
                });
            });

            col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Color.FromHex(SystemBurgundy));
        });
    }

    private void ComposePdfContent(IContainer container, Patient patient, List<AppointmentRecord> records)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(10).Column(c =>
            {
                c.Item().Text($"Paciente: {patient.Name}").FontSize(12).Bold();

                if (!string.IsNullOrWhiteSpace(patient.Cpf))
                    c.Item().Text($"CPF: {patient.Cpf}").FontSize(10);

                if (!string.IsNullOrWhiteSpace(patient.Phone))
                    c.Item().Text($"Telefone: {patient.Phone.FormatPhone()}").FontSize(10);

                if (!string.IsNullOrWhiteSpace(patient.Email))
                    c.Item().Text($"E-mail: {patient.Email}").FontSize(10);

                if (patient.BirthDate.HasValue)
                    c.Item().Text($"Nascimento: {patient.BirthDate:dd/MM/yyyy}").FontSize(10);

                if (!string.IsNullOrWhiteSpace(patient.StateOfResidency))
                    c.Item().Text($"Estado: {patient.StateOfResidency}").FontSize(10);
            });

            col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            if (records.Count == 0)
            {
                col.Item().PaddingTop(20).AlignCenter().Text("Nenhum prontuário encontrado.")
                    .FontSize(12).FontColor(Colors.Grey.Medium);
                return;
            }

            col.Item().Text($"Histórico de Sessões ({records.Count})")
                .FontSize(14).Bold().FontColor(Color.FromHex(SystemBurgundy));

            foreach (var record in records)
            {
                col.Item().PaddingVertical(6).Column(recordCol =>
                {
                    recordCol.Item().Background(Colors.Grey.Lighten5).Padding(8).Column(detail =>
                    {
                        var appointmentDate = record.Appointment?.AppointmentDate
                            .ToPortoVelhoTime().ToString("dd/MM/yyyy HH:mm") ?? "Data não informada";

                        detail.Item().Text($"Sessão — {appointmentDate}")
                            .FontSize(11).Bold().FontColor(Color.FromHex(SystemGreen));

                        if (!string.IsNullOrWhiteSpace(record.Note))
                        {
                            detail.Item().PaddingTop(4).Text(record.Note)
                                .FontSize(10).LineHeight(1.4f);
                        }

                        detail.Item().PaddingTop(4).Text($"Criado em: {record.CreatedAt.ToPortoVelhoTime():dd/MM/yyyy HH:mm}")
                            .FontSize(8).FontColor(Colors.Grey.Darken2);

                        if (record.UpdatedAt.HasValue)
                        {
                            detail.Item().Text($"Retificado em: {record.UpdatedAt.Value.ToPortoVelhoTime():dd/MM/yyyy HH:mm}")
                                .FontSize(8).FontColor(Color.FromHex(SystemBurgundy));
                        }
                    });

                    recordCol.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten3);
                });
            }
        });
    }

    private void ComposePdfFooter(IContainer container)
    {
        container.AlignCenter().Text(t =>
        {
            t.Span("Gerado pelo Sistema Ísis — Dados confidenciais\n")
                .FontSize(8).FontColor(Colors.Grey.Medium);
            t.Span($"Em {DateTimeOffset.UtcNow.ToPortoVelhoTime():dd/MM/yyyy 'às' HH:mm}")
                .FontSize(8).FontColor(Colors.Grey.Medium);
        });
    }

    private byte[] GenerateDocx(Patient patient, List<AppointmentRecord> records)
    {
        using var ms = new MemoryStream();
        using (var doc = PackOpenXml.WordprocessingDocument.Create(
            ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new OpenXml.Document();
        var body = mainPart.Document.AppendChild(new OpenXml.Body());

        SetDocxStyles(body);

        // Title
        AddDocxPara(body, "PRONTUÁRIO DO PACIENTE",
            bold: true, size: 40, color: SystemGreen, spacingAfter: 80);

        // Subtitle
        AddDocxPara(body,
            $"Gerado em: {DateTimeOffset.UtcNow.ToPortoVelhoTime():dd/MM/yyyy HH:mm}",
            size: 20, color: "808080", spacingAfter: 200);

        // Burgundy divider
        AddDocxDivider(body, SystemBurgundy);

        // Patient info section
        AddDocxPara(body, $"Paciente: {patient.Name}",
            bold: true, size: 24, spacingAfter: 120);

        if (!string.IsNullOrWhiteSpace(patient.Cpf))
            AddDocxPara(body, $"CPF: {patient.Cpf.FormatCpf()}", size: 20);
        if (!string.IsNullOrWhiteSpace(patient.Phone))
            AddDocxPara(body, $"Telefone: {patient.Phone.FormatPhone()}", size: 20);
        if (!string.IsNullOrWhiteSpace(patient.Email))
            AddDocxPara(body, $"E-mail: {patient.Email}", size: 20);
        if (patient.BirthDate.HasValue)
            AddDocxPara(body, $"Nascimento: {patient.BirthDate:dd/MM/yyyy}", size: 20);
        if (!string.IsNullOrWhiteSpace(patient.StateOfResidency))
            AddDocxPara(body, $"Estado: {patient.StateOfResidency}", size: 20);

        AddDocxSpacer(body);
        AddDocxDivider(body, "CCCCCC");
        AddDocxSpacer(body);

        // Records section
        if (records.Count == 0)
        {
            AddDocxPara(body, "Nenhum prontuário encontrado.",
                size: 22, color: "808080", spacingBefore: 200);
        }
        else
        {
            AddDocxPara(body, $"Histórico de Sessões ({records.Count})",
                bold: true, size: 28, color: SystemBurgundy, spacingAfter: 300);

            foreach (var record in records)
            {
                var appointmentDate = record.Appointment?.AppointmentDate
                    .ToPortoVelhoTime().ToString("dd/MM/yyyy HH:mm") ?? "Data não informada";

                // Each record wrapped in grey background
                AddDocxPara(body, $"Sessão — {appointmentDate}",
                    bold: true, size: 22, color: SystemGreen, spacingAfter: 100, shade: "F5F5F5");

                if (!string.IsNullOrWhiteSpace(record.Note))
                {
                    AddDocxPara(body, record.Note, size: 20, spacingAfter: 80, shade: "F5F5F5");
                }

                AddDocxPara(body,
                    $"Criado em: {record.CreatedAt.ToPortoVelhoTime():dd/MM/yyyy HH:mm}",
                    size: 16, color: "666666", spacingAfter: 40, shade: "F5F5F5");

                if (record.UpdatedAt.HasValue)
                {
                    AddDocxPara(body,
                        $"Retificado em: {record.UpdatedAt.Value.ToPortoVelhoTime():dd/MM/yyyy HH:mm}",
                        size: 16, color: SystemBurgundy, spacingAfter: 200, shade: "F5F5F5");
                }

                AddDocxDivider(body, "EEEEEE");
                AddDocxSpacer(body);
            }
        }

        // Footer
        AddDocxSpacer(body);
        AddDocxPara(body, "", spacingBefore: 240, spacingAfter: 60);

        AddDocxParaCentered(body, "Gerado pelo Sistema Ísis — Dados confidenciais",
            size: 16, color: "808080", spacingAfter: 40);

        AddDocxParaCentered(body,
            $"Em {DateTimeOffset.UtcNow.ToPortoVelhoTime():dd/MM/yyyy 'às' HH:mm}",
            size: 16, color: "808080");

        mainPart.Document.Save();
        }
        return ms.ToArray();
    }

    private static void SetDocxStyles(OpenXml.Body body)
    {
        var styles = new OpenXml.Styles(
            new OpenXml.DocDefaults(
                new OpenXml.RunPropertiesDefault(
                    new OpenXml.RunPropertiesBaseStyle(
                        new OpenXml.RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
                        new OpenXml.FontSize { Val = "20" }
                    )
                ),
                new OpenXml.ParagraphPropertiesDefault(
                    new OpenXml.ParagraphPropertiesBaseStyle(
                        new OpenXml.SpacingBetweenLines { After = "60", Line = "276", LineRule = OpenXml.LineSpacingRuleValues.Auto }
                    )
                )
            )
        );
        body.AppendChild(styles);
    }

    private static void AddDocxPara(OpenXml.Body body, string text,
        bool bold = false, int size = 20, string? color = null,
        int spacingBefore = 0, int spacingAfter = 0, string? shade = null)
    {
        var paraProps = new OpenXml.ParagraphProperties();

        if (spacingBefore > 0)
            paraProps.AppendChild(new OpenXml.SpacingBetweenLines { Before = spacingBefore.ToString() });

        if (spacingAfter > 0)
            paraProps.AppendChild(new OpenXml.SpacingBetweenLines { After = spacingAfter.ToString() });

        if (shade != null)
        {
            paraProps.AppendChild(new OpenXml.Shading
            {
                Val = OpenXml.ShadingPatternValues.Clear,
                Color = "auto",
                Fill = shade
            });
        }

        if (string.IsNullOrEmpty(text))
        {
            body.AppendChild(new OpenXml.Paragraph(paraProps, new OpenXml.Run()));
            return;
        }

        var runProps = new OpenXml.RunProperties(
            new OpenXml.RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
            new OpenXml.FontSize { Val = size.ToString() });

        if (bold)
            runProps.AppendChild(new OpenXml.Bold());

        if (!string.IsNullOrWhiteSpace(color))
            runProps.AppendChild(new OpenXml.Color { Val = color });

        var run = new OpenXml.Run(runProps,
            new OpenXml.Text(text) { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve });

        body.AppendChild(new OpenXml.Paragraph(paraProps, run));
    }

    private static void AddDocxParaCentered(OpenXml.Body body, string text,
        int size = 20, string? color = null, int spacingAfter = 0)
    {
        var paraProps = new OpenXml.ParagraphProperties(
            new OpenXml.Justification { Val = OpenXml.JustificationValues.Center });

        if (spacingAfter > 0)
            paraProps.AppendChild(new OpenXml.SpacingBetweenLines { After = spacingAfter.ToString() });

        var runProps = new OpenXml.RunProperties(
            new OpenXml.RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
            new OpenXml.FontSize { Val = size.ToString() });

        if (!string.IsNullOrWhiteSpace(color))
            runProps.AppendChild(new OpenXml.Color { Val = color });

        var run = new OpenXml.Run(runProps,
            new OpenXml.Text(text) { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve });

        body.AppendChild(new OpenXml.Paragraph(paraProps, run));
    }

    private static void AddDocxDivider(OpenXml.Body body, string color)
    {
        body.AppendChild(new OpenXml.Paragraph(
            new OpenXml.ParagraphProperties(
                new OpenXml.ParagraphBorders(
                    new OpenXml.BottomBorder
                    {
                        Val = OpenXml.BorderValues.Single,
                        Color = color,
                        Size = 6,
                        Space = 1
                    })),
            new OpenXml.Run()));
    }

    private static void AddDocxSpacer(OpenXml.Body body)
    {
        body.AppendChild(new OpenXml.Paragraph(new OpenXml.Run()));
    }
}
