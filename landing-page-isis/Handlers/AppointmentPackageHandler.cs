using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Handlers;

public class AppointmentPackageHandler(AppDbContext context) : IAppointmentPackageHandler
{
    public async Task<PaginatedResponse<AppointmentPackageListItemDto>> GetPackagesByPatientId(
        int page,
        int pageSize,
        Guid patientId,
        CancellationToken ct
    )
    {
        var query = context
            .AppointmentPackages.AsNoTracking()
            .Where(ap => ap.PatientId == patientId);
        var totalItems = await query.CountAsync(ct);

        if (totalItems <= 0)
            return new PaginatedResponse<AppointmentPackageListItemDto>(
                [],
                totalItems,
                page,
                pageSize
            );

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(p => new AppointmentPackageListItemDto(
                p.Id,
                p.PatientId,
                p.TotalAppointments,
                p.RemainingAppointments,
                p.Price,
                p.Status,
                p.CreatedAt,
                p.PaymentMethod
            ))
            .ToListAsync(ct);

        return new PaginatedResponse<AppointmentPackageListItemDto>(
            items,
            totalItems,
            page,
            pageSize
        );
    }

    public async Task<HandlerResult> CreatePackage(AppointmentPackage package)
    {
        if (package.PatientId == Guid.Empty)
            return new HandlerResult(false, "Paciente não informado.");

        if (package.TotalAppointments <= 0)
            return new HandlerResult(false, "O número total de consultas deve ser maior que zero.");

        if (package.Price <= 0)
            return new HandlerResult(false, "O preço deve ser maior que zero.");

        package.RemainingAppointments = package.TotalAppointments;
        package.CreatedAt = DateTimeOffset.UtcNow;

        context.AppointmentPackages.Add(package);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> UpdatePackage(AppointmentPackage package)
    {
        var existing = await context.AppointmentPackages.FindAsync(package.Id);
        if (existing == null)
            return new HandlerResult(false, "Pacote não encontrado.");

        existing.TotalAppointments = package.TotalAppointments;
        existing.RemainingAppointments = package.RemainingAppointments;
        existing.PaymentMethod = package.PaymentMethod;
        existing.Price = package.Price;
        existing.Status = package.Status;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> DeletePackage(Guid id)
    {
        var existing = await context.AppointmentPackages.FindAsync(id);
        if (existing == null)
            return new HandlerResult(false, "Pacote não encontrado.");

        context.AppointmentPackages.Remove(existing);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<List<AppointmentPackageListItemDto>> GetAllPackagesByDateRange(
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct
    )
    {
        if (!await context.AppointmentPackages.AnyAsync(ct))
            return [];

        return await context
            .AppointmentPackages.AsNoTracking()
            .Where(ap => ap.CreatedAt >= start.UtcDateTime && ap.CreatedAt <= end.UtcDateTime)
            .Select(p => new AppointmentPackageListItemDto(
                p.Id,
                p.PatientId,
                p.TotalAppointments,
                p.RemainingAppointments,
                p.Price,
                p.Status,
                p.CreatedAt,
                p.PaymentMethod
            ))
            .ToListAsync(ct);
    }
}
