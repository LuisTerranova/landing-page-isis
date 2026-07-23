using FluentValidation;
using landing_page_isis.core;
using landing_page_isis.core.Helpers;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;
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
        CancellationToken ct
    )
    {
        var query = context.Patients.AsNoTracking();
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
        CancellationToken ct
    )
    {
        var queryable = context.Patients.AsNoTracking();

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
}
