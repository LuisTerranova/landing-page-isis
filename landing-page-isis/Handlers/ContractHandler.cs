using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;
using landing_page_isis.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace landing_page_isis.Handlers;

public partial class ContractHandler(
    AppDbContext context,
    IHttpContextAccessor httpContextAccessor,
    IMemoryCache cache
) : IContractHandler
{
    private const int ContractRateLimit = 2;
    private static readonly TimeSpan ContractRateWindow = TimeSpan.FromMinutes(5);

    public async Task<PaginatedResponse<ContractListItemDto>> GetContracts(
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        var query = context.Contracts.AsNoTracking();
        var totalItems = await query.CountAsync(ct);

        if (totalItems <= 0)
            return new PaginatedResponse<ContractListItemDto>([], totalItems, page, pageSize);

        var rawItems = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.PatientName,
                c.Status,
                c.Type,
                c.Price,
                c.CreatedAt
            })
            .ToListAsync(ct);

        var items = rawItems.Select(c => new ContractListItemDto(
            c.Id,
            c.CreatedAt.ToString("yyMMdd") + "-" + c.Id.ToString().Substring(0, 4).ToUpper(),
            c.PatientName,
            c.Status,
            c.Type,
            c.Price,
            c.CreatedAt
        )).ToList();

        return new PaginatedResponse<ContractListItemDto>(items, totalItems, page, pageSize);
    }

    public async Task<Contract?> GetContract(Guid id)
    {
        return await context.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Contract?> GetContractByToken(string token)
    {
        return await context.Contracts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.AcceptanceToken == token);
    }

    public async Task<HandlerResult> CreateContract(Contract? contract)
    {
        if (contract == null)
            return new HandlerResult(false, "Dados inválidos.");

        var clientIp = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var cacheKey = $"contract-rate:{clientIp}";

        if (cache.TryGetValue(cacheKey, out int count) && count >= ContractRateLimit)
            return new HandlerResult(false, "Muitas solicitações. Tente novamente em alguns minutos.");

        cache.Set(cacheKey, count + 1, ContractRateWindow);

        if (string.IsNullOrWhiteSpace(contract.PatientName))
            return new HandlerResult(false, "Nome é obrigatório.");

        if (contract.PatientName.Length > 150)
            return new HandlerResult(false, "Nome deve ter no máximo 150 caracteres.");

        if (!string.IsNullOrEmpty(contract.PatientEmail) && !EmailRegex().IsMatch(contract.PatientEmail))
            return new HandlerResult(false, "E-mail inválido.");

        if (!string.IsNullOrEmpty(contract.PatientPhone))
        {
            contract.PatientPhone = OnlyNumbersRegex().Replace(contract.PatientPhone, "");
            if (contract.PatientPhone.Length < 10 || contract.PatientPhone.Length > 11)
                return new HandlerResult(false, "Telefone inválido. Deve ter 10 ou 11 dígitos.");
        }

        if (!string.IsNullOrEmpty(contract.PatientCpf))
        {
            contract.PatientCpf = OnlyNumbersRegex().Replace(contract.PatientCpf, "");
            if (contract.PatientCpf.Length != 11)
                return new HandlerResult(false, "CPF inválido. Deve ter 11 dígitos.");
        }

        if (!contract.TermsAccepted)
            return new HandlerResult(false, "É necessário aceitar os termos.");

        contract.CreatedAt = DateTimeOffset.UtcNow;
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> AcceptContract(string token)
    {
        var contract = await context.Contracts
            .FirstOrDefaultAsync(c => c.AcceptanceToken == token);

        if (contract == null)
            return new HandlerResult(false, "Link inválido.");

        if (contract.Status != ContractStatus.AguardandoAceitacao)
            return new HandlerResult(false, "Contrato não está aguardando aceitação.");

        if (contract.CreatedAt.AddDays(2) < DateTimeOffset.UtcNow)
            return new HandlerResult(false, "Link expirado. Entre em contato pelo WhatsApp (69) 99223-4931.");

        contract.Status = ContractStatus.Ativo;
        contract.AcceptedAt = DateTimeOffset.UtcNow;
        contract.UpdatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> UpdateContract(Contract contract)
    {
        var existing = await context.Contracts.FindAsync(contract.Id);
        if (existing == null)
            return new HandlerResult(false, "Contrato não encontrado.");

        existing.Price = contract.Price;
        existing.InitialAppointments = contract.InitialAppointments;
        existing.Type = contract.Type;
        existing.PackagePrice = contract.PackagePrice;
        existing.AcceptanceToken = contract.AcceptanceToken;
        existing.ContractDocumentHtml = contract.ContractDocumentHtml;
        existing.Status = contract.Status;
        existing.PackageId = contract.PackageId;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> DeleteContract(Guid id)
    {
        var contract = await context.Contracts.FindAsync(id);
        if (contract == null)
            return new HandlerResult(false, "Contrato não encontrado.");

        context.Contracts.Remove(contract);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"[^\d]")]
    private static partial System.Text.RegularExpressions.Regex OnlyNumbersRegex();

    [System.Text.RegularExpressions.GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial System.Text.RegularExpressions.Regex EmailRegex();
}
