using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;
using landing_page_isis.Extensions;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Handlers;

/// <summary>
/// Handles the scheduling, package deductions, session cancellations, and credit refunds for therapy appointments.
/// </summary>
public class AppointmentHandler(AppDbContext context) : IAppointmentHandler
{
    public async Task<PaginatedResponse<AppointmentListItemDto>> GetAllAppointments(
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        var query = context
            .Appointments.Include(a => a.Patient)
            .Include(a => a.Couple)
            .AsNoTracking();

        var totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.AppointmentDate)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(a => ToListItemDto(a))
            .ToListAsync(ct);

        return new PaginatedResponse<AppointmentListItemDto>(items, totalItems, page, pageSize);
    }

    public async Task<PaginatedResponse<AppointmentListItemDto>> GetAppointmentsByPatientId(
        int page,
        int pageSize,
        Guid patientId,
        CancellationToken ct
    )
    {
        var query = context
            .Appointments.Include(a => a.Patient)
            .Include(a => a.Couple)
            .AsNoTracking()
            .Where(a => a.PatientId == patientId);

        var totalItems = await query.CountAsync(ct);

        if (totalItems <= 0)
            return new PaginatedResponse<AppointmentListItemDto>([], totalItems, page, pageSize);

        var items = await query
            .OrderByDescending(a => a.AppointmentDate)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(a => ToListItemDto(a))
            .ToListAsync(ct);

        return new PaginatedResponse<AppointmentListItemDto>(items, totalItems, page, pageSize);
    }

    public async Task<Appointment?> GetAppointment(Guid id, Guid patientId)
    {
        return await context
            .Appointments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.PatientId == patientId);
    }

    public async Task<Appointment?> GetAppointmentWithPatient(Guid id, Guid patientId)
    {
        return await context
            .Appointments.Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == id && a.PatientId == patientId);
    }

    public async Task<Appointment?> GetAppointmentById(Guid id)
    {
        // Exclude patient/couple inclusions to optimize lightweight detail requests where relations aren't required
        return await context.Appointments.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<HandlerResult> CreateAppointment(Appointment appointment)
    {
        var validation = appointment.Validate();
        if (!validation.Success)
            return validation;

        // Force UTC conversion before database storage to satisfy PostgreSQL timestamp constraints
        appointment.AppointmentDate = appointment.AppointmentDate.ToUniversalTime();

        // Enforce time slot uniqueness to prevent booking conflicts
        var isOccupied = await context
            .Appointments.AsNoTracking()
            .AnyAsync(a => a.AppointmentDate == appointment.AppointmentDate);

        if (isOccupied)
            return new HandlerResult(false, "Este horário já possui um agendamento.");

        // Automatically deduct a session credit if the client has an active prepaid package
        AppointmentPackage? activePackage = null;

        if (appointment.PatientId.HasValue && appointment.PatientId.Value != Guid.Empty)
        {
            activePackage = await context
                .AppointmentPackages.Where(p =>
                    p.PatientId == appointment.PatientId
                    && p.Status == PackageStatus.Ativo
                    && p.RemainingAppointments > 0
                )
                .OrderBy(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }
        else if (appointment.CoupleId.HasValue)
        {
            activePackage = await context
                .AppointmentPackages.Where(p =>
                    p.CoupleId == appointment.CoupleId
                    && p.Status == PackageStatus.Ativo
                    && p.RemainingAppointments > 0
                )
                .OrderBy(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        if (activePackage != null)
        {
            activePackage.RemainingAppointments--;
            if (activePackage.RemainingAppointments <= 0)
                activePackage.Status = PackageStatus.Esgotado;

            appointment.PackageId = activePackage.Id;
            appointment.Price = 0; // Session is free since it is covered by the prepaid package
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

        var validation = appointment.Validate();
        if (!validation.Success)
            return validation;

        // Refund a package session if the appointment is being cancelled
        if (
            appointment.AppointmentStatus == AppointmentStatusEnum.Cancelada
            && existing.AppointmentStatus != AppointmentStatusEnum.Cancelada
            && existing.PackageId.HasValue
        )
        {
            await RefundPackageCredit(existing.PackageId.Value);
        }

        // Force UTC conversion before database storage to satisfy PostgreSQL timestamp constraints
        appointment.AppointmentDate = appointment.AppointmentDate.ToUniversalTime();

        // Enforce slot uniqueness (excluding the current record itself)
        var isOccupied = await context
            .Appointments.AsNoTracking()
            .AnyAsync(a =>
                a.Id != appointment.Id && a.AppointmentDate == appointment.AppointmentDate
            );

        if (isOccupied)
            return new HandlerResult(false, "Horario indisponivel");

        // SetValues is safe here because Appointment has fully mutable property mapping
        context.Entry(existing).CurrentValues.SetValues(appointment);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> DeleteAppointment(Guid id)
    {
        var app = await context.Appointments.FindAsync(id);
        if (app == null)
            return new HandlerResult(false, "Consulta não encontrada.");

        // Refund the package credit if this deleted session was paid for via a package
        if (app.PackageId.HasValue)
            await RefundPackageCredit(app.PackageId.Value);

        context.Appointments.Remove(app);
        await context.SaveChangesAsync();

        return new HandlerResult(true);
    }

    public async Task<List<AppointmentListItemDto>> GetAllAppointmentsByDateRange(
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct
    )
    {
        return await context
            .Appointments.AsNoTracking()
            .Include(a => a.Patient)
            .Where(a => a.AppointmentDate >= start && a.AppointmentDate <= end)
            .OrderBy(a => a.AppointmentDate)
            .Select(a => ToListItemDto(a))
            .ToListAsync(ct);
    }

    public async Task<List<AppointmentListItemDto>> GetAppointmentsByCoupleId(
        Guid coupleId,
        CancellationToken ct
    )
    {
        return await context
            .Appointments.AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Couple)
            .Where(a => a.CoupleId == coupleId)
            .OrderByDescending(a => a.AppointmentDate)
            .Select(a => ToListItemDto(a))
            .ToListAsync(ct);
    }

    public async Task<PaginatedResponse<AppointmentListItemDto>> GetAppointmentsByDateRange(
        DateTimeOffset start,
        DateTimeOffset end,
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        var query = context
            .Appointments.AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Couple)
            .Where(a => a.AppointmentDate >= start && a.AppointmentDate <= end);

        var totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderBy(a => a.AppointmentDate)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(a => ToListItemDto(a))
            .ToListAsync(ct);

        return new PaginatedResponse<AppointmentListItemDto>(items, totalItems, page, pageSize);
    }

    public async Task<PaginatedResponse<AppointmentListItemDto>> QueryAppointments(
        string query,
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        var queryable = context
            .Appointments.Include(a => a.Patient)
            .Include(a => a.Couple)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
            queryable = queryable.Where(a =>
                (a.Patient != null && a.Patient.Name.Contains(query))
                || (a.Couple != null && a.Couple.Name.Contains(query))
            );

        var totalItems = await queryable.CountAsync(ct);

        if (totalItems <= 0)
            return new PaginatedResponse<AppointmentListItemDto>([], totalItems, page, pageSize);

        var items = await queryable
            .OrderByDescending(a => a.AppointmentDate)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(a => ToListItemDto(a))
            .ToListAsync(ct);

        return new PaginatedResponse<AppointmentListItemDto>(items, totalItems, page, pageSize);
    }

    public async Task<int> CountPendingRecordsAsync()
    {
        var now = DateTimeOffset.UtcNow;
        
        // Count scheduled appointments in the past that do not have clinical note records written yet
        return await context
            .Appointments.AsNoTracking()
            .CountAsync(a =>
                a.AppointmentDate <= now && a.AppointmentStatus == AppointmentStatusEnum.Marcada
            );
    }

    private static AppointmentListItemDto ToListItemDto(Appointment a)
    {
        var displayName = a.Couple?.Name ?? a.Patient?.Name;
        return new AppointmentListItemDto(
            a.Id,
            a.AppointmentDate,
            a.PatientId,
            a.CoupleId,
            displayName,
            a.AppointmentStatus,
            a.Price,
            a.ReminderSent,
            a.PackageId
        );
    }

    /// <summary>
    /// Restores a prepaid session credit to a package and returns its state back to Active if it was marked as Exhausted.
    /// </summary>
    private async Task RefundPackageCredit(Guid packageId)
    {
        var package = await context.AppointmentPackages.FindAsync(packageId);
        if (package == null)
            return;

        package.RemainingAppointments++;

        if (package.Status == PackageStatus.Esgotado)
            package.Status = PackageStatus.Ativo;
    }
}
