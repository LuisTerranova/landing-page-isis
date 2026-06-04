using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Handlers;

public partial class CoupleHandler(AppDbContext context) : ICoupleHandler
{
    public async Task<PaginatedResponse<CoupleListItemDto>> GetCouples(
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        var query = context
            .Couples.Include(c => c.Patient1)
            .Include(c => c.Patient2)
            .AsNoTracking();

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
        if (string.IsNullOrWhiteSpace(couple.Name))
            return new HandlerResult(false, "Nome do casal é obrigatório.");

        if (couple.Name.Length > 150)
            return new HandlerResult(false, "Nome do casal deve ter no máximo 150 caracteres.");

        if (couple.Patient1Id == couple.Patient2Id)
            return new HandlerResult(false, "Os dois pacientes devem ser diferentes.");

        var alreadyInCouple = await context.Couples.AnyAsync(c =>
            c.Patient1Id == couple.Patient1Id
            || c.Patient2Id == couple.Patient1Id
            || c.Patient1Id == couple.Patient2Id
            || c.Patient2Id == couple.Patient2Id
        );

        if (alreadyInCouple)
            return new HandlerResult(false, "Um dos pacientes já pertence a outro casal.");

        if (!string.IsNullOrEmpty(couple.PayerCpf))
        {
            couple.PayerCpf = OnlyNumbersRegex().Replace(couple.PayerCpf, "");
            if (couple.PayerCpf.Length != 11)
                return new HandlerResult(false, "CPF do pagador inválido. Deve ter 11 dígitos.");
        }

        context.Couples.Add(couple);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> UpdateCouple(Couple couple)
    {
        var existing = await context.Couples.FindAsync(couple.Id);
        if (existing == null)
            return new HandlerResult(false, "Casal não encontrado.");

        if (string.IsNullOrWhiteSpace(couple.Name))
            return new HandlerResult(false, "Nome do casal é obrigatório.");

        if (couple.Name.Length > 150)
            return new HandlerResult(false, "Nome do casal deve ter no máximo 150 caracteres.");

        if (!string.IsNullOrEmpty(couple.PayerCpf))
        {
            couple.PayerCpf = OnlyNumbersRegex().Replace(couple.PayerCpf, "");
            if (couple.PayerCpf.Length != 11)
                return new HandlerResult(false, "CPF do pagador inválido. Deve ter 11 dígitos.");
        }

        context.Entry(existing).CurrentValues.SetValues(couple);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> DeleteCouple(Guid id)
    {
        var couple = await context.Couples.FindAsync(id);
        if (couple == null)
            return new HandlerResult(false, "Casal não encontrado.");

        context.Couples.Remove(couple);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<PaginatedResponse<CoupleListItemDto>> QueryCouples(
        string query,
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        var queryable = context
            .Couples.Include(c => c.Patient1)
            .Include(c => c.Patient2)
            .AsNoTracking();

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

    [System.Text.RegularExpressions.GeneratedRegex(@"[^\d]")]
    private static partial System.Text.RegularExpressions.Regex OnlyNumbersRegex();
}
