using FluentValidation;
using landing_page_isis.core;
using landing_page_isis.core.Helpers;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;
using landing_page_isis.Extensions;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Handlers;

/// <summary>
/// Handles CRUD operations, pagination queries, and metadata mapping for individual Patient profiles.
/// </summary>
public class PatientHandler(AppDbContext context, IValidator<Patient> validator) : IPatientHandler
{
    public async Task<PaginatedResponse<PatientListItemDto>> GetPatients(
        int page,
        int pageSize,
        CancellationToken ct,
        PatientStatusFilter filter = PatientStatusFilter.Active
    )
    {
        var query = context.Patients.AsNoTracking();

        query = filter switch
        {
            PatientStatusFilter.Active => query.Where(p => p.IsActive),
            PatientStatusFilter.Inactive => query.Where(p => !p.IsActive),
            _ => query,
        };

        var totalItems = await query.CountAsync(ct);

        if (totalItems <= 0)
            return new PaginatedResponse<PatientListItemDto>([], totalItems, page, pageSize);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(p => new PatientListItemDto(p.Id, p.Name, p.Email, p.Phone, p.StateOfResidency))
            .ToListAsync(ct);

        return new PaginatedResponse<PatientListItemDto>(items, totalItems, page, pageSize);
    }

    public async Task<Patient?> GetPatient(Guid id)
    {
        return await context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<HandlerResult> CreatePatient(Patient? patient)
    {
        if (patient == null)
            return new HandlerResult(false, "Dados inválidos.");

        var validation = await validator.ValidateAsync(patient);
        if (!validation.IsValid)
            return new HandlerResult(false, validation.Errors.First().ErrorMessage);

        if (!string.IsNullOrEmpty(patient.Phone))
            patient.Phone = CpfValidator.Strip(patient.Phone);

        if (!string.IsNullOrEmpty(patient.Cpf))
        {
            patient.Cpf = CpfValidator.Strip(patient.Cpf);
            patient.CpfHash = CpfHelper.ComputeHash(patient.Cpf);
        }

        context.Patients.Add(patient);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> UpdatePatient(Patient patient)
    {
        var existing = await context.Patients.FindAsync(patient.Id);
        if (existing == null)
            return new HandlerResult(false, "Paciente não encontrado.");

        var validation = await validator.ValidateAsync(patient);
        if (!validation.IsValid)
            return new HandlerResult(false, validation.Errors.First().ErrorMessage);

        if (!string.IsNullOrEmpty(patient.Phone))
            patient.Phone = CpfValidator.Strip(patient.Phone);

        if (!string.IsNullOrEmpty(patient.Cpf))
        {
            patient.Cpf = CpfValidator.Strip(patient.Cpf);
            patient.CpfHash = CpfHelper.ComputeHash(patient.Cpf);
        }
        else
        {
            patient.CpfHash = null;
        }

        context.Entry(existing).CurrentValues.SetValues(patient);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<Dictionary<Guid, string?>> GetPatientEmailMap(
        IEnumerable<Guid> ids,
        CancellationToken ct
    )
    {
        var distinctIds = ids.Distinct().ToList();

        // Bulk fetch patient emails in a single query to prevent N+1 performance issues in background workers
        return await context
            .Patients.Where(p => distinctIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Email })
            .ToDictionaryAsync(p => p.Id, p => p.Email, ct);
    }

    public async Task<HandlerResult> DeletePatient(Guid id)
    {
        var patient = await context.Patients.FindAsync(id);
        if (patient == null)
            return new HandlerResult(false, "Paciente não encontrado.");

        var couple = await context.Couples.AnyAsync(c => c.Patient1Id == id || c.Patient2Id == id);

        if (couple)
            return new HandlerResult(
                false,
                "Não é possível excluir este paciente porque ele faz parte de um casal. Remova o vínculo do casal antes de excluir."
            );

        context.Patients.Remove(patient);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<PaginatedResponse<PatientListItemDto>> QueryPatients(
        string query,
        int page,
        int pageSize,
        CancellationToken ct,
        bool excludeWithActiveContract = false,
        bool includeDeactivated = false
    )
    {
        var queryable = context.Patients.AsNoTracking();

        if (!includeDeactivated)
            queryable = queryable.Where(p => p.IsActive);

        if (excludeWithActiveContract)
        {
            queryable = queryable.Where(p =>
                !context.Contracts.Any(c =>
                    c.PatientId == p.Id && c.Status != ContractStatus.Cancelado
                )
                && !context.Couples.Any(c =>
                    (c.Patient1Id == p.Id || c.Patient2Id == p.Id)
                    && context.Contracts.Any(cc =>
                        cc.CoupleId == c.Id && cc.Status != ContractStatus.Cancelado
                    )
                )
            );
        }

        if (!string.IsNullOrWhiteSpace(query))
            queryable = queryable.Where(p => p.Name.Contains(query));

        var totalItems = await queryable.CountAsync(ct);

        if (totalItems <= 0)
            return new PaginatedResponse<PatientListItemDto>([], totalItems, page, pageSize);

        var items = await queryable
            .OrderBy(p => p.Name)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(p => new PatientListItemDto(p.Id, p.Name, p.Email, p.Phone, p.StateOfResidency))
            .ToListAsync(ct);

        return new PaginatedResponse<PatientListItemDto>(items, totalItems, page, pageSize);
    }

    public async Task<HandlerResult> SetPatientActive(Guid id, bool active, CancellationToken ct)
    {
        var patient = await context.Patients.FirstOrDefaultAsync(p => p.Id == id, ct);

        if (patient is null)
            return new HandlerResult(false, "Paciente não encontrado.");

        if (patient.IsActive == active)
            return new HandlerResult(true);

        if (!active)
        {
            var belongsToCouple = await context.Couples.AnyAsync(
                c => c.Patient1Id == id || c.Patient2Id == id,
                ct
            );

            if (belongsToCouple)
                return new HandlerResult(
                    false,
                    "Não é possível desativar este paciente porque ele pertence a um casal."
                );

            var now = DateTimeOffset.UtcNow;
            var hasFutureAppointment = await context.Appointments.AnyAsync(
                a =>
                    a.PatientId == id
                    && a.AppointmentStatus == AppointmentStatusEnum.Marcada
                    && a.AppointmentDate >= now,
                ct
            );

            if (hasFutureAppointment)
                return new HandlerResult(
                    false,
                    "Não é possível desativar este paciente porque ele possui consultas futuras agendadas."
                );

            var hasActivePackageWithBalance = await context.AppointmentPackages.AnyAsync(
                p =>
                    p.PatientId == id
                    && p.Status == PackageStatus.Ativo
                    && p.RemainingAppointments > 0,
                ct
            );

            if (hasActivePackageWithBalance)
                return new HandlerResult(
                    false,
                    "Não é possível desativar este paciente porque ele possui um pacote de consultas ativo com saldo."
                );
        }

        patient.IsActive = active;
        await context.SaveChangesAsync(ct);

        return new HandlerResult(
            true,
            active ? "Paciente reativado com sucesso." : "Paciente desativado com sucesso."
        );
    }

    public async Task<IReadOnlyList<BirthdayListItemDto>> GetUpcomingBirthdays(
        int daysAhead,
        CancellationToken ct
    )
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToPortoVelhoTime());

        var patients = await context
            .Patients.AsNoTracking()
            .Where(p => p.IsActive && p.BirthDate != null)
            .Select(p => new
            {
                p.Id,
                p.Name,
                BirthDate = p.BirthDate!.Value,
            })
            .ToListAsync(ct);

        var birthdays = new List<BirthdayListItemDto>();

        foreach (var patient in patients)
        {
            var nextBirthday = GetNextBirthday(patient.BirthDate, today);
            var daysUntil = nextBirthday.DayNumber - today.DayNumber;

            if (daysUntil < 0 || daysUntil > daysAhead)
                continue;

            birthdays.Add(
                new BirthdayListItemDto(
                    patient.Id,
                    patient.Name,
                    patient.BirthDate,
                    nextBirthday,
                    daysUntil,
                    daysUntil == 0
                )
            );
        }

        return birthdays
            .OrderBy(b => b.DaysUntil)
            .ThenBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static DateOnly GetNextBirthday(DateOnly birthDate, DateOnly today)
    {
        var candidate = ResolveBirthdayForYear(today.Year, birthDate);

        if (candidate < today)
            candidate = ResolveBirthdayForYear(today.Year + 1, birthDate);

        return candidate;
    }

    private static DateOnly ResolveBirthdayForYear(int year, DateOnly birthDate)
    {
        // February 29 birthdays are celebrated on February 28 in non-leap years
        if (birthDate.Month == 2 && birthDate.Day == 29 && !DateTime.IsLeapYear(year))
            return new DateOnly(year, 2, 28);

        return new DateOnly(year, birthDate.Month, birthDate.Day);
    }
}
