using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Handlers;

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

        if (!string.IsNullOrEmpty(patient.Phone))
            patient.Phone = OnlyNumbersRegex().Replace(patient.Phone, "");

        if (!string.IsNullOrEmpty(patient.Cpf))
            patient.Cpf = OnlyNumbersRegex().Replace(patient.Cpf, "");

        context.Patients.Add(patient);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> UpdatePatient(Patient patient)
    {
        var existing = await context.Patients.FindAsync(patient.Id);
        if (existing == null)
            return new HandlerResult(false, "Paciente não encontrado.");

        if (!string.IsNullOrEmpty(patient.Phone))
            patient.Phone = OnlyNumbersRegex().Replace(patient.Phone, "");

        if (!string.IsNullOrEmpty(patient.Cpf))
            patient.Cpf = OnlyNumbersRegex().Replace(patient.Cpf, "");

        context.Entry(existing).CurrentValues.SetValues(patient);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
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
}
