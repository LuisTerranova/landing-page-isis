using landing_page_isis.core.Models.DTOs;

namespace landing_page_isis.core.Interfaces;

/// <summary>
/// Handles standalone clinical notes attached to a patient or a couple.
/// </summary>
public interface IAdditionalNoteHandler
{
    /// <summary>
    /// Returns a non-deleted note by its identifier.
    /// </summary>
    Task<AdditionalNoteDto?> GetById(Guid id, CancellationToken ct);

    /// <summary>
    /// Creates a standalone note. Exactly one owner (patient or couple) must be provided.
    /// </summary>
    Task<HandlerResult> Create(
        Guid? patientId,
        Guid? coupleId,
        string content,
        DateTimeOffset noteDate,
        CancellationToken ct
    );

    /// <summary>
    /// Updates the content and event date of a non-deleted note.
    /// </summary>
    Task<HandlerResult> Update(
        Guid id,
        string content,
        DateTimeOffset noteDate,
        CancellationToken ct
    );

    /// <summary>
    /// Performs a logical deletion of the note.
    /// </summary>
    Task<HandlerResult> Delete(Guid id, CancellationToken ct);
}
