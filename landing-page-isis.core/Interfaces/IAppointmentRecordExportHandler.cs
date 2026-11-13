namespace landing_page_isis.core.Interfaces;

/// <summary>
/// Exports clinical record histories to PDF or DOCX.
/// </summary>
public interface IAppointmentRecordExportHandler
{
    /// <summary>
    /// Exports an individual patient's clinical history. Drafts are never exported.
    /// </summary>
    Task<byte[]> ExportPatientRecords(
        Guid patientId,
        string format,
        bool includeNotes = false,
        CancellationToken ct = default
    );

    /// <summary>
    /// Exports a couple's clinical history, including both partners' appointments. Drafts are never exported.
    /// </summary>
    Task<byte[]> ExportCoupleRecords(
        Guid coupleId,
        string format,
        bool includeNotes = false,
        CancellationToken ct = default
    );
}
