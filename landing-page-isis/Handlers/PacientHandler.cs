using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis;

public class PacientHandler(AppDbContext context) : IPacientHandler
{
    public async Task<PaginatedResponse<Pacient>> GetPacients(int page, int pageSize, CancellationToken ct)
    {
        var query = context.Pacients.AsNoTracking();
        var totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResponse<Pacient>(items, totalItems, page, pageSize);
    }

    public async Task<Pacient?> GetPacient(Guid id)
    {
        return await context.Pacients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<HandlerResult> CreatePacient(Pacient? pacient)
    {
        if (pacient == null) 
            return new HandlerResult(false, "Dados inválidos.");

        context.Pacients.Add(pacient);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> UpdatePacient(Pacient pacient)
    {
        var existing = await context.Pacients.FindAsync(pacient.Id);
        if (existing == null) 
            return new HandlerResult(false, "Paciente não encontrado.");

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
}