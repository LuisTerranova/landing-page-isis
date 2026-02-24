using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Handlers;

public class AppointmentHandler(AppDbContext context) : IAppointmentHandler
{
  public async Task<PaginatedResponse<Appointment?>> GetAllAppointments(int page, int pageSize, CancellationToken ct)
  {
    var query = context.Appointments
      .Include(a => a.Pacient)
      .AsNoTracking();
    
    var totalItems = await query.CountAsync(ct);

    var items = await query
      .OrderByDescending(a => a.AppointmentDate)
      .Skip(page * pageSize)
      .Take(pageSize)
      .ToListAsync(ct);
    
    return new PaginatedResponse<Appointment?>(items, totalItems, page, pageSize);
  }

  public async Task<PaginatedResponse<Appointment?>> GetAppointmentsByPacientId(int page, int pageSize
    ,Guid pacientId, CancellationToken ct)
  {
    var query = context.Appointments
        .Include(a => a.Pacient)
        .AsNoTracking()
        .Where(a => a.PacientId == pacientId);

    var totalItems = await query.CountAsync(ct); 
    
    //WIP add return if null here, with paginatedresponse and null items if count 0.

    var items = await query
        .OrderByDescending(a => a.AppointmentDate)
        .Skip(page * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

    return new PaginatedResponse<Appointment?>(items, totalItems, page, pageSize);
  }

  public async Task<Appointment?> GetAppointment(Guid id, Guid pacientId)
  {
    return await context.Appointments
        .AsNoTracking()
        .FirstOrDefaultAsync(a => a.Id == id && a.PacientId == pacientId);
  }

  public async Task<HandlerResult> CreateAppointment(Appointment appointment)
  {
    if (appointment == null)
      return new HandlerResult(false, "Dados inválidos.");

    // Normalize to UTC for PostgreSQL compatibility
    appointment.AppointmentDate = appointment.AppointmentDate.ToUniversalTime();

    var isOccupied = await context.Appointments
        .AnyAsync(a => a.AppointmentDate == appointment.AppointmentDate);

    if (isOccupied)
      return new HandlerResult(false, "Este horário já possui um agendamento.");


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
      return new HandlerResult(false, "Agendamento não encontrado.");

    // Normalize to UTC for PostgreSQL compatibility
    appointment.AppointmentDate = appointment.AppointmentDate.ToUniversalTime();

    var isOccupied = await context.Appointments
        .AnyAsync(a => a.Id != appointment.Id && a.AppointmentDate == appointment.AppointmentDate);

    if (isOccupied)
      return new HandlerResult(false, "Horario indisponivel");

    context.Entry(existing).CurrentValues.SetValues(appointment);
    await context.SaveChangesAsync();
    return new HandlerResult(true);
  }

  public async Task<HandlerResult> DeleteAppointment(Guid id)
  {
    var appointment = await context.Appointments.FindAsync(id);

    if (appointment == null)
      return new HandlerResult(false, "Agendamento não encontrado.");

    context.Appointments.Remove(appointment);
    await context.SaveChangesAsync();
    return new HandlerResult(true);
  }

  public async Task<List<Appointment>> GetAppointmentsByDateRange(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
  {
    return await context.Appointments
      .AsNoTracking()
      .Include(a => a.Pacient)
      .Where(a => a.AppointmentDate >= start && a.AppointmentDate <= end)
      .OrderBy(a => a.AppointmentDate)
      .ToListAsync(ct);
  }
}
