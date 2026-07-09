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
public partial class PatientHandler(AppDbContext context) : IPatientHandler
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

        if (string.IsNullOrWhiteSpace(patient.Name))
            return new HandlerResult(false, "Nome é obrigatório.");

        if (patient.Name.Length > 150)
            return new HandlerResult(false, "Nome deve ter no máximo 150 caracteres.");

        if (!string.IsNullOrEmpty(patient.Email) && !EmailRegex().IsMatch(patient.Email))
            return new HandlerResult(false, "E-mail inválido.");

        if (!string.IsNullOrEmpty(patient.Phone))
        {
            patient.Phone = OnlyNumbersRegex().Replace(patient.Phone, "");
            if (patient.Phone.Length < 10 || patient.Phone.Length > 11)
                return new HandlerResult(false, "Telefone inválido. Deve ter 10 ou 11 dígitos.");
        }

        if (!string.IsNullOrEmpty(patient.Cpf))
        {
            patient.Cpf = OnlyNumbersRegex().Replace(patient.Cpf, "");
            if (!CpfValidator.IsValid(patient.Cpf))
                return new HandlerResult(false, "CPF inválido.");
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

        if (string.IsNullOrWhiteSpace(patient.Name))
            return new HandlerResult(false, "Nome é obrigatório.");

        if (patient.Name.Length > 150)
            return new HandlerResult(false, "Nome deve ter no máximo 150 caracteres.");

        if (!string.IsNullOrEmpty(patient.Email) && !EmailRegex().IsMatch(patient.Email))
            return new HandlerResult(false, "E-mail inválido.");

        if (!string.IsNullOrEmpty(patient.Phone))
        {
            patient.Phone = OnlyNumbersRegex().Replace(patient.Phone, "");
            if (patient.Phone.Length < 10 || patient.Phone.Length > 11)
                return new HandlerResult(false, "Telefone inválido. Deve ter 10 ou 11 dígitos.");
        }

        if (!string.IsNullOrEmpty(patient.Cpf))
        {
            patient.Cpf = OnlyNumbersRegex().Replace(patient.Cpf, "");
            if (!CpfValidator.IsValid(patient.Cpf))
                return new HandlerResult(false, "CPF inválido.");
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

    [System.Text.RegularExpressions.GeneratedRegex(@"[^\d]")]
    private static partial System.Text.RegularExpressions.Regex OnlyNumbersRegex();

    [System.Text.RegularExpressions.GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial System.Text.RegularExpressions.Regex EmailRegex();
}
