using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;
using landing_page_isis.Extensions;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Handlers;

/// <summary>
/// Handles clinical session records, including history pagination, date-based filtering, and audit-safe rectifications.
/// </summary>
public class AppointmentRecordHandler(AppDbContext context) : IAppointmentRecordHandler
{
    public async Task<AppointmentRecord?> GetAppointmentRecordById(Guid id)
    {
        return await context
            .AppointmentRecords.AsNoTracking()
            .FirstOrDefaultAsync(ar => ar.Id == id);
    }

    public async Task<AppointmentRecord?> GetAppointmentRecordByAppointmentId(Guid appointmentId)
    {
        return await context
            .AppointmentRecords.AsNoTracking()
            .FirstOrDefaultAsync(ar => ar.AppointmentId == appointmentId);
    }

    public async Task<AppointmentRecord?> GetDraftByAppointmentId(
        Guid appointmentId,
        CancellationToken ct
    )
    {
        return await context
            .AppointmentRecords.AsNoTracking()
            .FirstOrDefaultAsync(ar => ar.AppointmentId == appointmentId && ar.IsDraft, ct);
    }

    public async Task<HandlerResult> SaveDraft(
        Guid appointmentId,
        string note,
        CancellationToken ct
    )
    {
        var appointment = await context
            .Appointments.Include(a => a.Record)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, ct);

        if (appointment is null)
            return new HandlerResult(false, "Consulta não encontrada.");

        if (appointment.AppointmentStatus == AppointmentStatusEnum.Realizada)
            return new HandlerResult(false, "Consulta já finalizada.");

        if (appointment.AppointmentStatus == AppointmentStatusEnum.Cancelada)
            return new HandlerResult(
                false,
                "Não é possível salvar rascunho de uma consulta cancelada."
            );

        var trimmedNote = note?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(trimmedNote))
        {
            // An empty draft means the professional cleared the automatic note.
            // Finalized records are never removed through the draft flow.
            if (appointment.Record is { IsDraft: true })
                context.AppointmentRecords.Remove(appointment.Record);

            await context.SaveChangesAsync(ct);
            return new HandlerResult(true);
        }

        if (appointment.Record is not null)
        {
            if (!appointment.Record.IsDraft)
                return new HandlerResult(false, "Consulta já finalizada.");

            appointment.Record.Note = trimmedNote;
            appointment.Record.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            var now = DateTimeOffset.UtcNow;
            context.AppointmentRecords.Add(
                new AppointmentRecord
                {
                    AppointmentId = appointmentId,
                    Note = trimmedNote,
                    IsDraft = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                }
            );
        }

        await context.SaveChangesAsync(ct);
        return new HandlerResult(true, "Rascunho salvo com sucesso.");
    }

    public async Task<PaginatedResponse<AppointmentRecordListItemDto>> GetRecordsByPatientId(
        int page,
        int pageSize,
        Guid patientId,
        DateTime? filterMonthYear,
        CancellationToken ct
    )
    {
        var query = context
            .AppointmentRecords.Include(ar => ar.Appointment)
            .AsNoTracking()
            .Where(ar => ar.Appointment != null && ar.Appointment.PatientId == patientId);

        if (filterMonthYear.HasValue)
        {
            // Convert target month boundaries to UTC range to align with PostgreSQL timestamp configurations
            var localStart = new DateTime(
                filterMonthYear.Value.Year,
                filterMonthYear.Value.Month,
                1,
                0,
                0,
                0,
                DateTimeKind.Local
            );
            var utcStart = localStart.ToUniversalTime();
            var utcEnd = localStart.AddMonths(1).ToUniversalTime();

            query = query.Where(ar =>
                ar.Appointment!.AppointmentDate >= utcStart
                && ar.Appointment.AppointmentDate < utcEnd
            );
        }

        var totalItems = await query.CountAsync(ct);

        if (totalItems <= 0)
            return new PaginatedResponse<AppointmentRecordListItemDto>(
                [],
                totalItems,
                page,
                pageSize
            );

        var items = await query
            .OrderByDescending(ar => ar.CreatedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(ar => new AppointmentRecordListItemDto(
                ar.Id,
                ar.AppointmentId,
                ar.Appointment != null ? ar.Appointment.AppointmentDate : null,
                ar.Note ?? string.Empty,
                ar.CreatedAt,
                ar.UpdatedAt
            ))
            .ToListAsync(ct);

        return new PaginatedResponse<AppointmentRecordListItemDto>(
            items,
            totalItems,
            page,
            pageSize
        );
    }

    public async Task<HandlerResult> CreateAppointmentRecord(AppointmentRecord record)
    {
        if (string.IsNullOrEmpty(record.Note))
            return new HandlerResult(false, "Nota de consulta não pode estar nula.");

        record.CreatedAt = DateTimeOffset.UtcNow;

        context.AppointmentRecords.Add(record);
        await context.SaveChangesAsync();
        return new HandlerResult(true, "Nota criada com sucesso.");
    }

    public async Task<HandlerResult> UpdateAppointmentRecord(AppointmentRecord record)
    {
        var existing = await context.AppointmentRecords.FindAsync(record.Id);

        if (existing == null)
            return new HandlerResult(false, "Nota não encontrada.");

        if (existing.IsDraft)
            return new HandlerResult(
                false,
                "Não é possível retificar um rascunho. Finalize a consulta primeiro."
            );

        // Psychological records must preserve historical integrity.
        // We append rectifications rather than overwriting existing text to comply with medical record auditing rules.
        existing.Note +=
            $"\n\nRetificado em {DateTimeOffset.UtcNow.ToPortoVelhoTime():dd/MM/yyyy HH:mm}:\n{record.Note}";
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();
        return new HandlerResult(true, "Retificação adicionada com sucesso");
    }
}
