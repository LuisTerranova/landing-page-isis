using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Handlers;

public class AppointmentHandler(AppDbContext context) : IAppointmentHandler
{
    public async Task<PaginatedResponse<Appointment?>> GetAllAppointments(
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        var query = context.Appointments.Include(a => a.Pacient).AsNoTracking();

        var totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.AppointmentDate)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResponse<Appointment?>(items, totalItems, page, pageSize);
    }

    public async Task<PaginatedResponse<Appointment?>> GetAppointmentsByPacientId(
        int page,
        int pageSize,
        Guid pacientId,
        CancellationToken ct
    )
    {
        var query = context
            .Appointments.Include(a => a.Pacient)
            .AsNoTracking()
            .Where(a => a.PacientId == pacientId);

        var totalItems = await query.CountAsync(ct);

        if (totalItems <= 0)
            return new PaginatedResponse<Appointment?>([], totalItems, page, pageSize);

        var items = await query
            .OrderByDescending(a => a.AppointmentDate)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResponse<Appointment?>(items, totalItems, page, pageSize);
    }

    public async Task<Appointment?> GetAppointment(Guid id, Guid pacientId)
    {
        return await context
            .Appointments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.PacientId == pacientId);
    }

    public async Task<HandlerResult> CreateAppointment(Appointment appointment)
    {
        if (appointment == null)
            return new HandlerResult(false, "Dados inválidos.");

        if (appointment.Price < 0)
            return new HandlerResult(false, "O preço não pode ser negativo.");

        if (appointment.PacientId == Guid.Empty)
            return new HandlerResult(false, "Selecione um paciente válido.");

        // Normalize to UTC for PostgreSQL compatibility
        appointment.AppointmentDate = appointment.AppointmentDate.ToUniversalTime();

        var isOccupied = await context
            .Appointments.AsNoTracking()
            .AnyAsync(a => a.AppointmentDate == appointment.AppointmentDate);

        if (isOccupied)
            return new HandlerResult(false, "Este horário já possui um agendamento.");

        var activePackage = await context.AppointmentPackages.FirstOrDefaultAsync(p =>
            p.PacientId == appointment.PacientId
            && p.Status == PackageStatus.Ativo
            && p.RemainingAppointments > 0
        );

        if (activePackage != null)
        {
            activePackage.RemainingAppointments--;
            if (activePackage.RemainingAppointments <= 0)
                activePackage.Status = PackageStatus.Esgotado;

            appointment.Price = 0; // Zera o valor individual pois a consulta é via pacote
        }

        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> UpdateAppointment(Appointment appointment, Guid id)
    {
        if (id != appointment.Id)
            return new HandlerResult(false, "Os IDs de referência não coincidem.");

        var existing = await context.Appointments.FindAsync(id);

        if (existing == null)
            return new HandlerResult(false, "Consulta não encontrada.");

        if (appointment.Price < 0)
            return new HandlerResult(false, "O preço não pode ser negativo.");

        if (appointment.PacientId == Guid.Empty)
            return new HandlerResult(false, "Selecione um paciente válido.");

        // Normalize to UTC for PostgreSQL compatibility
        appointment.AppointmentDate = appointment.AppointmentDate.ToUniversalTime();

        var isOccupied = await context
            .Appointments.AsNoTracking()
            .AnyAsync(a =>
                a.Id != appointment.Id && a.AppointmentDate == appointment.AppointmentDate
            );

        if (isOccupied)
            return new HandlerResult(false, "Horario indisponivel");

        context.Entry(existing).CurrentValues.SetValues(appointment);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> DeleteAppointment(Guid id)
    {
        var app = await context.Appointments.FindAsync(id);
        if (app == null)
            return new HandlerResult(false, "Consulta não encontrada.");

        context.Appointments.Remove(app);
        await context.SaveChangesAsync();

        return new HandlerResult(true);
    }

    public async Task<List<Appointment>> GetAllAppointmentsByDateRange(
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct
    )
    {
        return await context
            .Appointments.AsNoTracking()
            .Include(a => a.Pacient)
            .Where(a => a.AppointmentDate >= start && a.AppointmentDate <= end)
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync(ct);
    }

    public async Task<PaginatedResponse<Appointment?>> GetAppointmentsByDateRange(
        DateTimeOffset start,
        DateTimeOffset end,
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        var query = context
            .Appointments.AsNoTracking()
            .Include(a => a.Pacient)
            .Where(a => a.AppointmentDate >= start && a.AppointmentDate <= end);

        var totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderBy(a => a.AppointmentDate)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResponse<Appointment?>(items, totalItems, page, pageSize);
    }

    public async Task<PaginatedResponse<Appointment?>> QueryAppointments(
        string query,
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        var queryable = context.Appointments.Include(a => a.Pacient).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
            queryable = queryable.Where(a => a.Pacient != null && a.Pacient.Name.Contains(query));

        var totalItems = await queryable.CountAsync(ct);

        if (totalItems <= 0)
            return new PaginatedResponse<Appointment?>([], totalItems, page, pageSize);

        var items = await queryable
            .OrderByDescending(a => a.AppointmentDate)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResponse<Appointment?>(items, totalItems, page, pageSize);
    }
}
