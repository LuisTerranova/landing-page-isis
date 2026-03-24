using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Handlers;

public partial class PacientHandler(AppDbContext context) : IPacientHandler
{
    public async Task<PaginatedResponse<Pacient?>> GetPacients(
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        var query = context.Pacients.AsNoTracking();
        var totalItems = await query.CountAsync(ct);

        if (totalItems <= 0)
            return new PaginatedResponse<Pacient?>([], totalItems, page, pageSize);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResponse<Pacient?>(items, totalItems, page, pageSize);
    }

    public async Task<Pacient?> GetPacient(Guid id)
    {
        return await context.Pacients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<HandlerResult> CreatePacient(Pacient? pacient)
    {
        if (pacient == null)
            return new HandlerResult(false, "Dados inválidos.");

        if (!string.IsNullOrEmpty(pacient.Phone))
            pacient.Phone = OnlyNumbersRegex().Replace(pacient.Phone, "");

        if (!string.IsNullOrEmpty(pacient.Cpf))
            pacient.Cpf = OnlyNumbersRegex().Replace(pacient.Cpf, "");

        context.Pacients.Add(pacient);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> UpdatePacient(Pacient pacient)
    {
        var existing = await context.Pacients.FindAsync(pacient.Id);
        if (existing == null)
            return new HandlerResult(false, "Paciente não encontrado.");

        if (!string.IsNullOrEmpty(pacient.Phone))
            pacient.Phone = OnlyNumbersRegex().Replace(pacient.Phone, "");

        if (!string.IsNullOrEmpty(pacient.Cpf))
            pacient.Cpf = OnlyNumbersRegex().Replace(pacient.Cpf, "");

        context.Entry(existing).CurrentValues.SetValues(pacient);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> DeletePacient(Guid id)
    {
        var pacient = await context.Pacients.FindAsync(id);
        if (pacient == null)
            return new HandlerResult(false, "Paciente não encontrado.");

        context.Pacients.Remove(pacient);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<PaginatedResponse<Pacient?>> QueryPacients(
        string query,
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        var queryable = context.Pacients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
            queryable = queryable.Where(p => p.Name.Contains(query));

        var totalItems = await queryable.CountAsync(ct);

        if (totalItems <= 0)
            return new PaginatedResponse<Pacient?>([], totalItems, page, pageSize);

        var items = await queryable
            .OrderBy(p => p.Name)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResponse<Pacient?>(items, totalItems, page, pageSize);
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"[^\d]")]
    private static partial System.Text.RegularExpressions.Regex OnlyNumbersRegex();
}
