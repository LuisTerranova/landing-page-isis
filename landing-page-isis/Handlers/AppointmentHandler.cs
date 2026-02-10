using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis;

public class AppointmentHandler(AppDbContext context) : IAppointmentHandler
{
  public async Task<PaginatedResponse<Appointment?>> GetAppointments(int page, int pageSize, Guid pacientId)
  {
    var query = context.Appointments
        .AsNoTracking()
        .Where(a => a.PacientId == pacientId);

    var totalItems = await query.CountAsync();

    var items = await query
        .OrderByDescending(a => a.AppointmentDate)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

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
}
