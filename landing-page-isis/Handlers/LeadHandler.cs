using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;
using landing_page_isis.Extensions;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Handlers;

/// <summary>
/// Handles patient leads lifecycle operations, including onboarding validation, approval/promotion to Patient entity, periodic purging, and WhatsApp link generation.
/// </summary>
public partial class LeadHandler(
    AppDbContext context,
    IHttpContextAccessor httpContextAccessor
) : ILeadHandler
{

    public async Task<PaginatedResponse<LeadListItemDto>> GetLeads(
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        var query = context.Leads.AsNoTracking();
        var totalItems = await query.CountAsync(ct);

        if (totalItems <= 0)
            return new PaginatedResponse<LeadListItemDto>([], totalItems, page, pageSize);

        var items = await query
            .OrderByDescending(l => l.Created)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(l => new LeadListItemDto(
                l.Id,
                l.Name,
                l.Email,
                l.Phone,
                l.Intent,
                l.Created,
                l.LeadStatus,
                l.PolicySigned
            ))
            .ToListAsync(ct);

        return new PaginatedResponse<LeadListItemDto>(items, totalItems, page, pageSize);
    }

    public async Task<Lead?> GetLead(Guid id)
    {
        return await context.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<HandlerResult> CreateLead(Lead lead)
    {
        if (lead == null)
            return new HandlerResult(false, "Dados não podem ser nulos.");

        var ip = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!await RateLimiterHelper.CheckAsync($"lead:{ip}", 3, TimeSpan.FromMinutes(1)))
            return new HandlerResult(false, "Muitas tentativas. Tente novamente mais tarde.");

        if (!string.IsNullOrEmpty(lead.Phone))
            lead.Phone = landing_page_isis.core.Helpers.CpfValidator.Strip(lead.Phone);

        context.Leads.Add(lead);
        await context.SaveChangesAsync();

        return new HandlerResult(true);
    }

    public async Task<HandlerResult> ApproveLead(Guid id)
    {
        // Use a database transaction to atomically transition the lead status and create the active Patient record
        await using var transaction = await context.Database.BeginTransactionAsync();

        var rowsAffected = await context
            .Leads.Where(l => l.Id == id && l.LeadStatus == LeadStatusEnum.Novo)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(l => l.LeadStatus, LeadStatusEnum.Aprovado)
            );

        if (rowsAffected > 0)
        {
            var lead = await context.Leads.AsNoTracking().FirstAsync(l => l.Id == id);
            context.Patients.Add(
                new Patient
                {
                    Name = lead.Name,
                    Email = lead.Email,
                    Phone = lead.Phone,
                    PolicySigned = lead.PolicySigned,
                }
            );
            await context.SaveChangesAsync();
        }

        await transaction.CommitAsync();

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

    public async Task<HandlerResult> CleanLeads(CancellationToken ct)
    {
        var markAsExpiredDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-15));
        var deleteDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3));

        // Leads marked as New automatically transition to Expired after 15 days of inactivity
        var leadsExpirados = await context
            .Leads.Where(l => l.LeadStatus == LeadStatusEnum.Novo && l.Created <= markAsExpiredDate)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(l => l.LeadStatus, LeadStatusEnum.Expirado),
                ct
            );

        // Permanently delete expired leads from the database after 3 months to protect client data privacy
        var leadsDeletados = await context
            .Leads.Where(l => l.LeadStatus == LeadStatusEnum.Expirado && l.Created <= deleteDate)
            .ExecuteDeleteAsync(ct);

        var message =
            $"{leadsExpirados} leads marcados como expirados e {leadsDeletados} leads removidos permanentemente.";

        return new HandlerResult(true, message);
    }

    public string GetWhatsAppUrl(Lead lead)
    {
        if (string.IsNullOrWhiteSpace(lead.Phone))
            return string.Empty;

        // Clean phone number
        var cleanPhone = landing_page_isis.core.Helpers.CpfValidator.Strip(lead.Phone);

        // Add Brazil country code (55)
        if (cleanPhone.Length == 10 || cleanPhone.Length == 11)
            cleanPhone = "55" + cleanPhone;

        // Determine greeting based on current time in Brasilia
        var brazilTime = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")
        );
        var hour = brazilTime.Hour;

        // Dynamic greeting according to the time of day in Brazil
        var greeting = hour switch
        {
            >= 5 and < 12 => "Bom dia",
            >= 12 and < 18 => "Boa tarde",
            _ => "Boa noite",
        };

        var firstName = lead.Name.Split(' ').FirstOrDefault() ?? "paciente";

        var text =
            $"{greeting}, {firstName}! Tudo bem? Me chamo Isis Vitória. Vi que você deixou seu contato no site e fico feliz pelo seu interesse. Como posso te auxiliar nesse primeiro momento?";
        var encodedText = Uri.EscapeDataString(text);

        // Build WhatsApp click-to-chat API redirection link
        return $"https://wa.me/{cleanPhone}?text={encodedText}";
    }
}
