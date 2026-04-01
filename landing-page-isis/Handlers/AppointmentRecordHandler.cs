using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.Data;
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

    public async Task<AppointmentRecord?> GetAppointmentRecordByAppointmentId(Guid appointmentId)
    {
        return await context
            .AppointmentRecords.AsNoTracking()
            .FirstOrDefaultAsync(ar => ar.AppointmentId == appointmentId);
    }

    public async Task<HandlerResult> CreateAppointmentRecord(AppointmentRecord record)
    {
        // Additional validation could be checked here
        if (record.AppointmentId == Guid.Empty)
            return new HandlerResult(false, "Consulta não informada.");

        record.CreatedAt = DateTime.Now;

        context.AppointmentRecords.Add(record);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> UpdateAppointmentRecord(AppointmentRecord record)
    {
        var existing = await context.AppointmentRecords.FindAsync(record.Id);
        if (existing == null)
            return new HandlerResult(false, "Prontuário não encontrado.");

        existing.Note = record.Note;
        existing.UpdatedAt = DateTime.Now;

        context.Entry(existing).CurrentValues.SetValues(existing);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }
}
