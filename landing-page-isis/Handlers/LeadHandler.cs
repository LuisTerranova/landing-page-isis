using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace landing_page_isis.Handlers;

public partial class LeadHandler(
    AppDbContext context,
    IHttpContextAccessor httpContextAccessor,
    IMemoryCache cache
) : ILeadHandler
{
    private const string RateLimitPrefix = "lead_rate_limit_";
    private string? RemoteIp => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public async Task<PaginatedResponse<Lead?>> GetLeads(
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        var query = context.Leads.AsNoTracking();
        var totalItems = await query.CountAsync(ct);

        if (totalItems <= 0)
            return new PaginatedResponse<Lead?>([], totalItems, page, pageSize);

        var items = await query
            .OrderByDescending(l => l.Created)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResponse<Lead?>(items, totalItems, page, pageSize);
    }

    public async Task<Lead?> GetLead(Guid id)
    {
        return await context.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<HandlerResult> CreateLead(Lead lead)
    {
        if (lead == null)
            return new HandlerResult(false, "Dados não podem ser nulos.");

        // Check Rate Limit
        if (!string.IsNullOrEmpty(RemoteIp))
        {
            var cacheKey = $"{RateLimitPrefix}{RemoteIp}";
            if (cache.TryGetValue(cacheKey, out int attempts) && attempts >= 3)
            {
                return new HandlerResult(
                    false,
                    "Muitas tentativas. Por favor, aguarde um minuto antes de enviar novamente."
                );
            }

            // Increment attempts
            attempts++;
            cache.Set(cacheKey, attempts, TimeSpan.FromMinutes(1));
        }

        if (!string.IsNullOrEmpty(lead.Phone))
            lead.Phone = OnlyNumbersRegex().Replace(lead.Phone, "");

        context.Leads.Add(lead);
        await context.SaveChangesAsync();

        return new HandlerResult(true);
    }

    public async Task<HandlerResult> ApproveLead(Guid id)
    {
        var rowsAffected = await context
            .Leads.Where(l => l.Id == id)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(l => l.LeadStatus, LeadStatusEnum.Aprovado)
            );

        if (rowsAffected > 0)
        {
            var lead = await context.Leads.AsNoTracking().FirstAsync(l => l.Id == id);
            context.Pacients.Add(
                new Pacient
                {
                    Name = lead.Name,
                    Email = lead.Email,
                    Phone = lead.Phone,
                    PolicySigned = lead.PolicySigned,
                }
            );
            await context.SaveChangesAsync();
        }

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

        // Mark 15 days old leads as expired
        var leadsExpirados = await context
            .Leads.Where(l => l.LeadStatus == LeadStatusEnum.Novo && l.Created <= markAsExpiredDate)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(l => l.LeadStatus, LeadStatusEnum.Expirado),
                ct
            );

        // Delete leads that are expired for more than 3 months
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
        var cleanPhone = OnlyNumbersRegex().Replace(lead.Phone, "");

        // Add Brazil country code (55)
        if (cleanPhone.Length == 10 || cleanPhone.Length == 11)
            cleanPhone = "55" + cleanPhone;

        // Determine greeting based on current time in Brasilia
        var brazilTime = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")
        );
        var hour = brazilTime.Hour;

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

        return $"https://wa.me/{cleanPhone}?text={encodedText}";
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"[^\d]")]
    private static partial System.Text.RegularExpressions.Regex OnlyNumbersRegex();
}
