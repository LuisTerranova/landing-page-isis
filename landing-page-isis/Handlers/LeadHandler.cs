using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis;

public partial class LeadHandler(AppDbContext context) : ILeadHandler
{
    public async Task<PaginatedResponse<Lead?>> GetLeads(int page = 1, int pageSize = 5)
    {
        var query = context.Leads.AsNoTracking();
        var totalItems = await query.CountAsync();

        if (totalItems <= 0) return null;

        var items = await query
            .OrderByDescending(l => l.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<Lead?>(items, totalItems, page, pageSize);
    }

    public async Task<Lead?> GetLead(Guid id)
    {
        return await context.Leads
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<HandlerResult> CreateLead(Lead lead)
    {
        if (lead == null)
            return new HandlerResult(false, "Dados nao podem ser nulos.");

        if (!string.IsNullOrEmpty(lead.Phone))
            lead.Phone = OnlyNumbersRegex().Replace(lead.Phone, "");

        context.Leads.Add(lead);
        await context.SaveChangesAsync();

        return new HandlerResult(true);
    }

    public async Task<HandlerResult> ApproveLead(Guid id)
    {
        var rowsAffected = await context.Leads
            .Where(l => l.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(l => l.LeadStatus, LeadStatusEnum.Approved));

        return rowsAffected == 0 
            ? new HandlerResult(false, "Lead não encontrado.") 
            : new HandlerResult(true);
    }

    public async Task<HandlerResult> DeleteLead(Guid id)
    {
        var lead = await context.Leads.FindAsync(id);

        if (lead == null)
            return new HandlerResult(false, "Lead nao encontrado.");

        context.Leads.Remove(lead);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"[^\d]")]
    private static partial System.Text.RegularExpressions.Regex OnlyNumbersRegex();
}