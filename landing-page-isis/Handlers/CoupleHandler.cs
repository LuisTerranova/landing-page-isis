using FluentValidation;
using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Handlers;

/// <summary>
/// Handles registration and lookup of couples for therapy, ensuring patients are uniquely mapped and not duplicated across other couple units.
/// </summary>
public class CoupleHandler(AppDbContext context, IValidator<Couple> validator) : ICoupleHandler
{
    public async Task<PaginatedResponse<CoupleListItemDto>> GetCouples(
        int page,
        int pageSize,
        CancellationToken ct,
        CoupleStatusFilter filter = CoupleStatusFilter.Active
    )
    {
        var query = ApplyStatusFilter(
            context.Couples.Include(c => c.Patient1).Include(c => c.Patient2).AsNoTracking(),
            filter
        );

        var totalItems = await query.CountAsync(ct);

        if (totalItems <= 0)
            return new PaginatedResponse<CoupleListItemDto>([], totalItems, page, pageSize);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(c => new CoupleListItemDto(
                c.Id,
                c.Name,
                c.Patient1.Name,
                c.Patient2.Name,
                c.Patient1.Phone,
                c.Patient1.Email
            ))
            .ToListAsync(ct);

        return new PaginatedResponse<CoupleListItemDto>(items, totalItems, page, pageSize);
    }

    public async Task<Couple?> GetCouple(Guid id)
    {
        return await context
            .Couples.Include(c => c.Patient1)
            .Include(c => c.Patient2)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<HandlerResult> CreateCouple(Couple couple)
    {
        var validation = await validator.ValidateAsync(couple);
        if (!validation.IsValid)
            return new HandlerResult(false, validation.Errors.First().ErrorMessage);

        // Enforce that a patient cannot belong to more than one couple concurrently
        var alreadyInCouple = await context.Couples.AnyAsync(c =>
            c.Patient1Id == couple.Patient1Id
            || c.Patient2Id == couple.Patient1Id
            || c.Patient1Id == couple.Patient2Id
            || c.Patient2Id == couple.Patient2Id
        );

        if (alreadyInCouple)
            return new HandlerResult(false, "Um dos pacientes já pertence a outro casal.");

        if (!string.IsNullOrEmpty(couple.PayerCpf))
            couple.PayerCpf = landing_page_isis.core.Helpers.CpfValidator.Strip(couple.PayerCpf);

        context.Couples.Add(couple);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> UpdateCouple(Couple couple)
    {
        var existing = await context.Couples.FindAsync(couple.Id);
        if (existing == null)
            return new HandlerResult(false, "Casal não encontrado.");

        var validation = await validator.ValidateAsync(couple);
        if (!validation.IsValid)
            return new HandlerResult(false, validation.Errors.First().ErrorMessage);

        // Enforce that a patient cannot belong to more than one couple concurrently (excluding this couple)
        var alreadyInCouple = await context.Couples.AnyAsync(c =>
            c.Id != couple.Id
            && (
                c.Patient1Id == couple.Patient1Id
                || c.Patient2Id == couple.Patient1Id
                || c.Patient1Id == couple.Patient2Id
                || c.Patient2Id == couple.Patient2Id
            )
        );

        if (alreadyInCouple)
            return new HandlerResult(false, "Um dos pacientes já pertence a outro casal.");

        if (!string.IsNullOrEmpty(couple.PayerCpf))
            couple.PayerCpf = landing_page_isis.core.Helpers.CpfValidator.Strip(couple.PayerCpf);

        // Activation status changes go through SetCoupleActive; profile updates must not toggle it.
        couple.IsActive = existing.IsActive;

        context.Entry(existing).CurrentValues.SetValues(couple);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> DeleteCouple(Guid id)
    {
        var couple = await context.Couples.FindAsync(id);
        if (couple == null)
            return new HandlerResult(false, "Casal não encontrado.");

        // Hardening: a couple with any linked history must be deactivated instead of deleted.
        // The AdditionalNote FK cascades on the database, so deleting would silently drop notes.
        var hasLinkedHistory =
            await context.Appointments.AnyAsync(a => a.CoupleId == id)
            || await context.AppointmentPackages.AnyAsync(p => p.CoupleId == id)
            || await context.AdditionalNotes.AnyAsync(n => n.CoupleId == id)
            || await context.Contracts.AnyAsync(c => c.CoupleId == id);

        if (hasLinkedHistory)
            return new HandlerResult(
                false,
                "Não é possível excluir este casal porque existem consultas, pacotes, notas ou contratos vinculados. Desative o casal em vez de excluí-lo."
            );

        context.Couples.Remove(couple);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> SetCoupleActive(Guid id, bool active, CancellationToken ct)
    {
        var couple = await context
            .Couples.Include(c => c.Patient1)
            .Include(c => c.Patient2)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (couple is null)
            return new HandlerResult(false, "Casal não encontrado.");

        if (couple.IsActive == active)
            return new HandlerResult(true);

        if (active)
        {
            var inactivePartners = new[] { couple.Patient1, couple.Patient2 }
                .Where(p => p is { IsActive: false })
                .Select(p => p.Name)
                .ToList();

            if (inactivePartners.Count > 0)
                return new HandlerResult(
                    false,
                    $"Reative os parceiros ({string.Join(", ", inactivePartners)}) antes de reativar o casal."
                );
        }
        else
        {
            var now = DateTimeOffset.UtcNow;

            var hasFutureAppointment = await context.Appointments.AnyAsync(
                a =>
                    a.CoupleId == id
                    && a.AppointmentStatus == AppointmentStatusEnum.Marcada
                    && a.AppointmentDate >= now,
                ct
            );

            if (hasFutureAppointment)
                return new HandlerResult(
                    false,
                    "Não é possível desativar este casal porque ele possui consultas futuras agendadas."
                );

            var hasActivePackageWithBalance = await context.AppointmentPackages.AnyAsync(
                p =>
                    p.CoupleId == id
                    && p.Status == PackageStatus.Ativo
                    && p.RemainingAppointments > 0,
                ct
            );

            if (hasActivePackageWithBalance)
                return new HandlerResult(
                    false,
                    "Não é possível desativar este casal porque ele possui um pacote de consultas ativo com saldo."
                );

            var hasOpenContract = await context.Contracts.AnyAsync(
                c =>
                    c.CoupleId == id
                    && (
                        c.Status == ContractStatus.Ativo
                        || c.Status == ContractStatus.AguardandoAceitacao
                    ),
                ct
            );

            if (hasOpenContract)
                return new HandlerResult(
                    false,
                    "Não é possível desativar este casal porque ele possui um contrato ativo ou aguardando aceitação."
                );
        }

        couple.IsActive = active;
        await context.SaveChangesAsync(ct);

        return new HandlerResult(
            true,
            active ? "Casal reativado com sucesso." : "Casal desativado com sucesso."
        );
    }

    public async Task<PaginatedResponse<CoupleListItemDto>> QueryCouples(
        string query,
        int page,
        int pageSize,
        CancellationToken ct,
        bool excludeWithActiveContract = false,
        CoupleStatusFilter filter = CoupleStatusFilter.Active
    )
    {
        var queryable = ApplyStatusFilter(
            context.Couples.Include(c => c.Patient1).Include(c => c.Patient2).AsNoTracking(),
            filter
        );

        if (excludeWithActiveContract)
        {
            queryable = queryable.Where(c =>
                !context.Contracts.Any(contract =>
                    contract.CoupleId == c.Id && contract.Status != ContractStatus.Cancelado
                )
                && !context.Contracts.Any(contract =>
                    contract.PatientId == c.Patient1Id
                    && contract.Status != ContractStatus.Cancelado
                )
                && !context.Contracts.Any(contract =>
                    contract.PatientId == c.Patient2Id
                    && contract.Status != ContractStatus.Cancelado
                )
            );
        }

        if (!string.IsNullOrWhiteSpace(query))
            queryable = queryable.Where(c => c.Name.Contains(query));

        var totalItems = await queryable.CountAsync(ct);

        if (totalItems <= 0)
            return new PaginatedResponse<CoupleListItemDto>([], totalItems, page, pageSize);

        var items = await queryable
            .OrderBy(c => c.Name)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(c => new CoupleListItemDto(
                c.Id,
                c.Name,
                c.Patient1.Name,
                c.Patient2.Name,
                c.Patient1.Phone,
                c.Patient1.Email
            ))
            .ToListAsync(ct);

        return new PaginatedResponse<CoupleListItemDto>(items, totalItems, page, pageSize);
    }

    public async Task<Couple?> GetCoupleByPatientId(Guid patientId)
    {
        return await context
            .Couples.Include(c => c.Patient1)
            .Include(c => c.Patient2)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Patient1Id == patientId || c.Patient2Id == patientId);
    }

    public async Task<List<CoupleListItemDto>> GetAllCouples(CancellationToken ct)
    {
        return await context
            .Couples.Include(c => c.Patient1)
            .Include(c => c.Patient2)
            .AsNoTracking()
            .Select(c => new CoupleListItemDto(
                c.Id,
                c.Name,
                c.Patient1.Name,
                c.Patient2.Name,
                c.Patient1.Phone,
                c.Patient1.Email
            ))
            .ToListAsync(ct);
    }

    private static IQueryable<Couple> ApplyStatusFilter(
        IQueryable<Couple> queryable,
        CoupleStatusFilter filter
    )
    {
        return filter switch
        {
            CoupleStatusFilter.Active => queryable.Where(c => c.IsActive),
            CoupleStatusFilter.Inactive => queryable.Where(c => !c.IsActive),
            _ => queryable,
        };
    }
}
