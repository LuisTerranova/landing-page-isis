using FluentValidation;
using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Handlers;

/// <summary>
/// Handles creation, edition, reading, and logical deletion of standalone clinical notes.
/// </summary>
public class AdditionalNoteHandler(AppDbContext context, IValidator<AdditionalNote> validator)
    : IAdditionalNoteHandler
{
    public async Task<AdditionalNoteDto?> GetById(Guid id, CancellationToken ct)
    {
        return await context
            .AdditionalNotes.AsNoTracking()
            .Where(n => n.Id == id && !n.IsDeleted)
            .Select(n => new AdditionalNoteDto(
                n.Id,
                n.PatientId,
                n.CoupleId,
                n.Content,
                n.NoteDate,
                n.CreatedAt,
                n.UpdatedAt
            ))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<HandlerResult> Create(
        Guid? patientId,
        Guid? coupleId,
        string content,
        DateTimeOffset noteDate,
        CancellationToken ct
    )
    {
        var note = new AdditionalNote
        {
            PatientId = patientId,
            CoupleId = coupleId,
            Content = content ?? string.Empty,
            NoteDate = noteDate,
        };

        var validation = await validator.ValidateAsync(note, ct);
        if (!validation.IsValid)
            return new HandlerResult(false, validation.Errors.First().ErrorMessage);

        note.CreatedAt = DateTimeOffset.UtcNow;

        context.AdditionalNotes.Add(note);
        await context.SaveChangesAsync(ct);
        return new HandlerResult(true, "Nota criada com sucesso.");
    }

    public async Task<HandlerResult> Update(
        Guid id,
        string content,
        DateTimeOffset noteDate,
        CancellationToken ct
    )
    {
        var note = await context.AdditionalNotes.FirstOrDefaultAsync(
            n => n.Id == id && !n.IsDeleted,
            ct
        );

        if (note is null)
            return new HandlerResult(false, "Nota não encontrada.");

        // Validate on a candidate instance so the tracked entity is never left with invalid values
        var candidate = new AdditionalNote
        {
            PatientId = note.PatientId,
            CoupleId = note.CoupleId,
            Content = content ?? string.Empty,
            NoteDate = noteDate,
        };

        var validation = await validator.ValidateAsync(candidate, ct);
        if (!validation.IsValid)
            return new HandlerResult(false, validation.Errors.First().ErrorMessage);

        note.Content = candidate.Content;
        note.NoteDate = candidate.NoteDate;
        note.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(ct);
        return new HandlerResult(true, "Nota atualizada com sucesso.");
    }

    public async Task<HandlerResult> Delete(Guid id, CancellationToken ct)
    {
        var note = await context.AdditionalNotes.FirstOrDefaultAsync(
            n => n.Id == id && !n.IsDeleted,
            ct
        );

        if (note is null)
            return new HandlerResult(false, "Nota não encontrada.");

        note.IsDeleted = true;
        note.DeletedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(ct);
        return new HandlerResult(true, "Nota excluída com sucesso.");
    }
}
