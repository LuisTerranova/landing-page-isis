using DocumentFormat.OpenXml;
using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models.DTOs;
using landing_page_isis.Extensions;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OpenXml = DocumentFormat.OpenXml.Wordprocessing;
using PackOpenXml = DocumentFormat.OpenXml.Packaging;

namespace landing_page_isis.Handlers;

/// <summary>
/// Handles exporting patient and couple clinical record histories into PDF (via QuestPDF) and DOCX (via OpenXML) formats.
/// </summary>
public class AppointmentRecordExportHandler(
    AppDbContext context,
    IProntuarioHandler prontuarioHandler
) : IAppointmentRecordExportHandler
{
    private static readonly string SystemGreen = "#2E7D32";
    private static readonly string SystemBurgundy = "#800020";

    public async Task<byte[]> ExportPatientRecords(
        Guid patientId,
        string format,
        bool includeNotes = false,
        CancellationToken ct = default
    )
    {
        var patient = await context
            .Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId, ct);

        if (patient is null)
            throw new KeyNotFoundException("Paciente não encontrado.");

        var items = await prontuarioHandler.GetFullTimelineByPatientId(
            patientId,
            includeNotes,
            includeDrafts: false,
            ct
        );

        var ownerDetails = new List<string>();

        if (!string.IsNullOrWhiteSpace(patient.Cpf))
            ownerDetails.Add($"CPF: {patient.Cpf.FormatCpf()}");

        if (!string.IsNullOrWhiteSpace(patient.Phone))
            ownerDetails.Add($"Telefone: {patient.Phone.FormatPhone()}");

        if (!string.IsNullOrWhiteSpace(patient.Email))
            ownerDetails.Add($"E-mail: {patient.Email}");

        if (patient.BirthDate.HasValue)
            ownerDetails.Add($"Nascimento: {patient.BirthDate:dd/MM/yyyy}");

        if (!string.IsNullOrWhiteSpace(patient.StateOfResidency))
            ownerDetails.Add($"Estado: {patient.StateOfResidency}");

        var export = new ExportContext(
            "PRONTUÁRIO DO PACIENTE",
            $"Paciente: {patient.Name}",
            ownerDetails,
            items
        );

        return Render(export, format);
    }

    public async Task<byte[]> ExportCoupleRecords(
        Guid coupleId,
        string format,
        bool includeNotes = false,
        CancellationToken ct = default
    )
    {
        var couple = await context
            .Couples.AsNoTracking()
            .Include(c => c.Patient1)
            .Include(c => c.Patient2)
            .FirstOrDefaultAsync(c => c.Id == coupleId, ct);

        if (couple is null)
            throw new KeyNotFoundException("Casal não encontrado.");

        var items = await prontuarioHandler.GetFullTimelineByCoupleId(
            coupleId,
            includeNotes,
            includeDrafts: false,
            ct
        );

        var ownerDetails = new List<string>
        {
            $"Parceiro(a): {couple.Patient1.Name}",
            $"Parceiro(a): {couple.Patient2.Name}",
        };

        var export = new ExportContext(
            "PRONTUÁRIO DO CASAL",
            $"Casal: {couple.Name}",
            ownerDetails,
            items
        );

        return Render(export, format);
    }

    private static byte[] Render(ExportContext export, string format)
    {
        return format.ToLowerInvariant() switch
        {
            "pdf" => GeneratePdf(export),
            "docx" => GenerateDocx(export),
            _ => throw new ArgumentException("Formato inválido. Use 'pdf' ou 'docx'."),
        };
    }

    private static string GetItemLabel(ProntuarioTimelineItemDto item)
    {
        return item.Type == ProntuarioItemType.Consulta ? "Consulta" : "Nota Avulsa";
    }

    private static string GetUpdatedLabel(ProntuarioTimelineItemDto item)
    {
        return item.Type == ProntuarioItemType.Consulta ? "Retificado em" : "Atualizado em";
    }

    private static byte[] GeneratePdf(ExportContext export)
    {
        // Build A4 PDF documents using QuestPDF's fluent API page layout engine
        return Document
            .Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);

                    page.Header().Element(c => ComposePdfHeader(c, export.Title));
                    page.Content().Element(c => ComposePdfContent(c, export));
                    page.Footer().Element(ComposePdfFooter);
                });
            })
            .GeneratePdf();
    }

    private static void ComposePdfHeader(IContainer container, string title)
    {
        container.Column(col =>
        {
            col.Item()
                .Row(row =>
                {
                    row.RelativeItem()
                        .Column(c =>
                        {
                            c.Item()
                                .Text(title)
                                .FontSize(20)
                                .Bold()
                                .FontColor(Color.FromHex(SystemGreen));

                            c.Item()
                                .Text(
                                    $"Gerado em: {DateTimeOffset.UtcNow.ToPortoVelhoTime():dd/MM/yyyy HH:mm}"
                                )
                                .FontSize(10)
                                .FontColor(Colors.Grey.Medium);
                        });
                });

            col.Item()
                .PaddingVertical(4)
                .LineHorizontal(1)
                .LineColor(Color.FromHex(SystemBurgundy));
        });
    }

    private static void ComposePdfContent(IContainer container, ExportContext export)
    {
        container.Column(col =>
        {
            col.Item()
                .PaddingTop(10)
                .Column(c =>
                {
                    c.Item().Text(export.OwnerHeading).FontSize(12).Bold();

                    foreach (var detail in export.OwnerDetails)
                        c.Item().Text(detail).FontSize(10);
                });

            col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            if (export.Items.Count == 0)
            {
                col.Item()
                    .PaddingTop(20)
                    .AlignCenter()
                    .Text("Nenhum prontuário encontrado.")
                    .FontSize(12)
                    .FontColor(Colors.Grey.Medium);
                return;
            }

            col.Item()
                .Text($"Histórico do Prontuário ({export.Items.Count})")
                .FontSize(14)
                .Bold()
                .FontColor(Color.FromHex(SystemBurgundy));

            foreach (var item in export.Items)
            {
                col.Item()
                    .PaddingVertical(6)
                    .Column(recordCol =>
                    {
                        recordCol
                            .Item()
                            .Background(Colors.Grey.Lighten5)
                            .Padding(8)
                            .Column(detail =>
                            {
                                var eventDate = item
                                    .EventDate.ToPortoVelhoTime()
                                    .ToString("dd/MM/yyyy HH:mm");

                                detail
                                    .Item()
                                    .Text($"{GetItemLabel(item)} — {eventDate}")
                                    .FontSize(11)
                                    .Bold()
                                    .FontColor(Color.FromHex(SystemGreen));

                                if (!string.IsNullOrWhiteSpace(item.Content))
                                {
                                    detail
                                        .Item()
                                        .PaddingTop(4)
                                        .Text(item.Content)
                                        .FontSize(10)
                                        .LineHeight(1.4f);
                                }

                                detail
                                    .Item()
                                    .PaddingTop(4)
                                    .Text(
                                        $"Criado em: {item.CreatedAt.ToPortoVelhoTime():dd/MM/yyyy HH:mm}"
                                    )
                                    .FontSize(8)
                                    .FontColor(Colors.Grey.Darken2);

                                if (item.UpdatedAt.HasValue)
                                {
                                    detail
                                        .Item()
                                        .Text(
                                            $"{GetUpdatedLabel(item)}: {item.UpdatedAt.Value.ToPortoVelhoTime():dd/MM/yyyy HH:mm}"
                                        )
                                        .FontSize(8)
                                        .FontColor(Color.FromHex(SystemBurgundy));
                                }
                            });

                        recordCol
                            .Item()
                            .PaddingVertical(2)
                            .LineHorizontal(1)
                            .LineColor(Colors.Grey.Lighten3);
                    });
            }
        });
    }

    private static void ComposePdfFooter(IContainer container)
    {
        container
            .AlignCenter()
            .Text(t =>
            {
                t.Span("Gerado pelo Sistema Ísis — Dados confidenciais\n")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Medium);
                t.Span($"Em {DateTimeOffset.UtcNow.ToPortoVelhoTime():dd/MM/yyyy 'às' HH:mm}")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Medium);
            });
    }

    private static byte[] GenerateDocx(ExportContext export)
    {
        // Programmatically construct Word documents (DOCX) using the OpenXML standard.
        using var ms = new MemoryStream();
        using (
            var doc = PackOpenXml.WordprocessingDocument.Create(
                ms,
                DocumentFormat.OpenXml.WordprocessingDocumentType.Document
            )
        )
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new OpenXml.Document();
            var body = mainPart.Document.AppendChild(new OpenXml.Body());

            SetDocxStyles(body);

            // Title
            AddDocxPara(
                body,
                export.Title,
                bold: true,
                size: 40,
                color: SystemGreen,
                spacingAfter: 80
            );

            // Subtitle
            AddDocxPara(
                body,
                $"Gerado em: {DateTimeOffset.UtcNow.ToPortoVelhoTime():dd/MM/yyyy HH:mm}",
                size: 20,
                color: "808080",
                spacingAfter: 200
            );

            // Burgundy divider
            AddDocxDivider(body, SystemBurgundy);

            // Owner info section
            AddDocxPara(body, export.OwnerHeading, bold: true, size: 24, spacingAfter: 120);

            foreach (var detail in export.OwnerDetails)
                AddDocxPara(body, detail, size: 20);

            AddDocxSpacer(body);
            AddDocxDivider(body, "CCCCCC");
            AddDocxSpacer(body);

            // Timeline section
            if (export.Items.Count == 0)
            {
                AddDocxPara(
                    body,
                    "Nenhum prontuário encontrado.",
                    size: 22,
                    color: "808080",
                    spacingBefore: 200
                );
            }
            else
            {
                AddDocxPara(
                    body,
                    $"Histórico do Prontuário ({export.Items.Count})",
                    bold: true,
                    size: 28,
                    color: SystemBurgundy,
                    spacingAfter: 300
                );

                foreach (var item in export.Items)
                {
                    var eventDate = item.EventDate.ToPortoVelhoTime().ToString("dd/MM/yyyy HH:mm");

                    // Each record wrapped in grey background
                    AddDocxPara(
                        body,
                        $"{GetItemLabel(item)} — {eventDate}",
                        bold: true,
                        size: 22,
                        color: SystemGreen,
                        spacingAfter: 100,
                        shade: "F5F5F5"
                    );

                    if (!string.IsNullOrWhiteSpace(item.Content))
                    {
                        AddDocxPara(
                            body,
                            item.Content,
                            size: 20,
                            spacingAfter: 80,
                            shade: "F5F5F5"
                        );
                    }

                    AddDocxPara(
                        body,
                        $"Criado em: {item.CreatedAt.ToPortoVelhoTime():dd/MM/yyyy HH:mm}",
                        size: 16,
                        color: "666666",
                        spacingAfter: 40,
                        shade: "F5F5F5"
                    );

                    if (item.UpdatedAt.HasValue)
                    {
                        AddDocxPara(
                            body,
                            $"{GetUpdatedLabel(item)}: {item.UpdatedAt.Value.ToPortoVelhoTime():dd/MM/yyyy HH:mm}",
                            size: 16,
                            color: SystemBurgundy,
                            spacingAfter: 200,
                            shade: "F5F5F5"
                        );
                    }

                    AddDocxDivider(body, "EEEEEE");
                    AddDocxSpacer(body);
                }
            }

            // Footer
            AddDocxSpacer(body);
            AddDocxPara(body, "", spacingBefore: 240, spacingAfter: 60);

            AddDocxParaCentered(
                body,
                "Gerado pelo Sistema Ísis — Dados confidenciais",
                size: 16,
                color: "808080",
                spacingAfter: 40
            );

            AddDocxParaCentered(
                body,
                $"Em {DateTimeOffset.UtcNow.ToPortoVelhoTime():dd/MM/yyyy 'às' HH:mm}",
                size: 16,
                color: "808080"
            );

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
                        new OpenXml.SpacingBetweenLines
                        {
                            After = "60",
                            Line = "276",
                            LineRule = OpenXml.LineSpacingRuleValues.Auto,
                        }
                    )
                )
            )
        );
        body.AppendChild(styles);
    }

    private static void AddDocxPara(
        OpenXml.Body body,
        string text,
        bool bold = false,
        int size = 20,
        string? color = null,
        int spacingBefore = 0,
        int spacingAfter = 0,
        string? shade = null
    )
    {
        // OpenXML requires separating ParagraphProperties (spacing, shading) and wrapping textual runs in RunProperties (font sizes, weights)
        var paraProps = new OpenXml.ParagraphProperties();

        if (spacingBefore > 0)
            paraProps.AppendChild(
                new OpenXml.SpacingBetweenLines { Before = spacingBefore.ToString() }
            );

        if (spacingAfter > 0)
            paraProps.AppendChild(
                new OpenXml.SpacingBetweenLines { After = spacingAfter.ToString() }
            );

        if (shade != null)
        {
            paraProps.AppendChild(
                new OpenXml.Shading
                {
                    Val = OpenXml.ShadingPatternValues.Clear,
                    Color = "auto",
                    Fill = shade,
                }
            );
        }

        if (string.IsNullOrEmpty(text))
        {
            body.AppendChild(new OpenXml.Paragraph(paraProps, new OpenXml.Run()));
            return;
        }

        var runProps = new OpenXml.RunProperties(
            new OpenXml.RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
            new OpenXml.FontSize { Val = size.ToString() }
        );

        if (bold)
            runProps.AppendChild(new OpenXml.Bold());

        if (!string.IsNullOrWhiteSpace(color))
            runProps.AppendChild(new OpenXml.Color { Val = color });

        var run = new OpenXml.Run(
            runProps,
            new OpenXml.Text(text)
            {
                Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve,
            }
        );

        body.AppendChild(new OpenXml.Paragraph(paraProps, run));
    }

    private static void AddDocxParaCentered(
        OpenXml.Body body,
        string text,
        int size = 20,
        string? color = null,
        int spacingAfter = 0
    )
    {
        var paraProps = new OpenXml.ParagraphProperties(
            new OpenXml.Justification { Val = OpenXml.JustificationValues.Center }
        );

        if (spacingAfter > 0)
            paraProps.AppendChild(
                new OpenXml.SpacingBetweenLines { After = spacingAfter.ToString() }
            );

        var runProps = new OpenXml.RunProperties(
            new OpenXml.RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
            new OpenXml.FontSize { Val = size.ToString() }
        );

        if (!string.IsNullOrWhiteSpace(color))
            runProps.AppendChild(new OpenXml.Color { Val = color });

        var run = new OpenXml.Run(
            runProps,
            new OpenXml.Text(text)
            {
                Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve,
            }
        );

        body.AppendChild(new OpenXml.Paragraph(paraProps, run));
    }

    private static void AddDocxDivider(OpenXml.Body body, string color)
    {
        // Render horizontal dividers by applying bottom borders directly to empty paragraph structures
        body.AppendChild(
            new OpenXml.Paragraph(
                new OpenXml.ParagraphProperties(
                    new OpenXml.ParagraphBorders(
                        new OpenXml.BottomBorder
                        {
                            Val = OpenXml.BorderValues.Single,
                            Color = color,
                            Size = 6,
                            Space = 1,
                        }
                    )
                ),
                new OpenXml.Run()
            )
        );
    }

    private static void AddDocxSpacer(OpenXml.Body body)
    {
        body.AppendChild(new OpenXml.Paragraph(new OpenXml.Run()));
    }

    private sealed record ExportContext(
        string Title,
        string OwnerHeading,
        IReadOnlyList<string> OwnerDetails,
        IReadOnlyList<ProntuarioTimelineItemDto> Items
    );
}
