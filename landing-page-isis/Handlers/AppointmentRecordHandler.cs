using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.Data;
using landing_page_isis.Extensions;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Handlers;

public class AppointmentRecordHandler(AppDbContext context) : IAppointmentRecordHandler
{
    public async Task<AppointmentRecord?> GetAppointmentRecordById(Guid id)
    {
        return await context
            .AppointmentRecords.AsNoTracking()
            .FirstOrDefaultAsync(ar => ar.Id == id);
    }

    public async Task<PaginatedResponse<AppointmentRecord?>> GetRecordsByPacientId(
        int page,
        int pageSize,
        Guid pacientId,
        DateTime? filterMonthYear,
        CancellationToken ct
    )
    {
        var query = context
            .AppointmentRecords.Include(ar => ar.Appointment)
            .AsNoTracking()
            .Where(ar => ar.Appointment != null && ar.Appointment.PacientId == pacientId);

        if (filterMonthYear.HasValue)
        {
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
            return new PaginatedResponse<AppointmentRecord?>([], totalItems, page, pageSize);

        var items = await query
            .OrderByDescending(ar => ar.CreatedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResponse<AppointmentRecord?>(items, totalItems, page, pageSize);
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

        existing.Note +=
            $"\n\nRetificado em {DateTimeOffset.UtcNow.ToPortoVelhoTime():dd/MM/yyyy HH:mm}:\n{record.Note}";
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        context.Entry(existing).CurrentValues.SetValues(existing);
        await context.SaveChangesAsync();
        return new HandlerResult(true, "Retificação adicionada com sucesso");
    }
}
