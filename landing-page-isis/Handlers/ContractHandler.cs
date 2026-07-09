using landing_page_isis.core;
using landing_page_isis.core.Helpers;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Handlers;

/// <summary>
/// Handles patient/couple service contracts, signature link generation and validation, CPF cryptographic checks, and contract-to-patient conversion.
/// </summary>
public partial class ContractHandler(AppDbContext context) : IContractHandler
{
    private static readonly string[] ValidUfs =
    [
        "AC",
        "AL",
        "AP",
        "AM",
        "BA",
        "CE",
        "DF",
        "ES",
        "GO",
        "MA",
        "MT",
        "MS",
        "MG",
        "PA",
        "PB",
        "PR",
        "PE",
        "PI",
        "RJ",
        "RN",
        "RS",
        "RO",
        "RR",
        "SC",
        "SP",
        "SE",
        "TO",
    ];

    public async Task<PaginatedResponse<ContractListItemDto>> GetContracts(
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        var query = context.Contracts.Include(c => c.Couple).AsNoTracking();

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
                c.CreatedAt,
                c.PatientId,
                c.CoupleId,
                CoupleName = c.Couple != null ? c.Couple.Name : null,
            })
            .ToListAsync(ct);

        var items = rawItems
            .Select(c => new ContractListItemDto(
                c.Id,
                // Generate a user-friendly contract identifier prefixing creation date (e.g., "260709-A1B2")
                c.CreatedAt.ToString("yyMMdd") + "-" + c.Id.ToString().Substring(0, 4).ToUpper(),
                c.PatientName,
                c.Status,
                c.Type,
                c.Price,
                c.CreatedAt,
                c.PatientId,
                c.CoupleId,
                c.CoupleName
            ))
            .ToList();

        return new PaginatedResponse<ContractListItemDto>(items, totalItems, page, pageSize);
    }

    public async Task<Contract?> GetContract(Guid id)
    {
        return await context.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Contract?> GetContractByToken(string token)
    {
        return await context
            .Contracts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.AcceptanceToken == token);
    }

    public async Task<HandlerResult> CreateContract(Contract? contract)
    {
        if (contract == null)
            return new HandlerResult(false, "Dados inválidos.");

        if (string.IsNullOrWhiteSpace(contract.PatientName))
            return new HandlerResult(false, "Nome é obrigatório.");

        if (contract.PatientName.Length > 150)
            return new HandlerResult(false, "Nome deve ter no máximo 150 caracteres.");

        if (
            !string.IsNullOrEmpty(contract.PatientEmail)
            && !EmailRegex().IsMatch(contract.PatientEmail)
        )
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
            if (!CpfValidator.IsValid(contract.PatientCpf))
                return new HandlerResult(false, "CPF inválido.");
        }

        if (
            !string.IsNullOrEmpty(contract.PatientState)
            && !ValidUfs.Contains(contract.PatientState.ToUpperInvariant())
        )
        {
            return new HandlerResult(false, "Estado (UF) inválido.");
        }

        if (
            contract.PatientBirthDate.HasValue
            && contract.PatientBirthDate.Value > DateOnly.FromDateTime(DateTime.Today)
        )
        {
            return new HandlerResult(false, "Data de nascimento inválida.");
        }

        if (!contract.TermsAccepted)
            return new HandlerResult(false, "É necessário aceitar os termos.");

        if (!string.IsNullOrEmpty(contract.PatientCpf))
        {
            // Compute cryptographic hash of CPF for privacy-safe querying and indexing
            var cpfHash = CpfHelper.ComputeHash(contract.PatientCpf);

            // Verify CPF uniqueness in pending/active contracts
            var existingInContracts = await context.Contracts.AnyAsync(c =>
                c.PatientCpfHash == cpfHash && c.Status != ContractStatus.Cancelado
            );

            if (existingInContracts)
                return new HandlerResult(false, "Já existe um cadastro com este CPF.");

            // Verify CPF uniqueness in already registered patients
            var existingInPatients = await context.Patients.AnyAsync(p => p.CpfHash == cpfHash);

            if (existingInPatients)
                return new HandlerResult(false, "Já existe um cadastro com este CPF.");

            contract.PatientCpfHash = cpfHash;
        }

        contract.CreatedAt = DateTimeOffset.UtcNow;
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> AcceptContract(string token, string? documentHtml = null)
    {
        var contract = await context.Contracts.FirstOrDefaultAsync(c => c.AcceptanceToken == token);

        if (contract == null)
            return new HandlerResult(false, "Link inválido ou expirado.");

        if (contract.Status != ContractStatus.AguardandoAceitacao)
            return new HandlerResult(false, "Contrato não está aguardando aceitação.");

        // Links automatically expire 2 days after generation to ensure contract terms freshness
        var expirationDate = (contract.TokenGeneratedAt ?? contract.CreatedAt).AddDays(2);
        if (expirationDate < DateTimeOffset.UtcNow)
            return new HandlerResult(
                false,
                "Link inválido ou expirado. Entre em contato pelo WhatsApp (69) 99223-4931."
            );

        contract.Status = ContractStatus.Ativo;
        contract.AcceptedAt = DateTimeOffset.UtcNow;
        contract.UpdatedAt = DateTimeOffset.UtcNow;

        if (documentHtml != null)
            contract.ContractDocumentHtml = documentHtml;

        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> UpdateContract(Contract contract)
    {
        var existing = await context.Contracts.FindAsync(contract.Id);
        if (existing == null)
            return new HandlerResult(false, "Contrato não encontrado.");

        if (existing.Status is ContractStatus.Ativo or ContractStatus.Cancelado)
            return new HandlerResult(false, "Não é possível alterar um contrato já finalizado.");

        // Force transition to Active status to happen only through AcceptContract (initiated by the patient)
        if (contract.Status == ContractStatus.Ativo)
            return new HandlerResult(false, "Status Ativo só pode ser definido pelo paciente ao aceitar o contrato.");

        // Manual mapping is preserved here to prevent SetValues from overwriting uneditable fields with default/null values
        existing.Price = contract.Price;
        existing.AcceptanceToken = contract.AcceptanceToken;
        existing.ContractDocumentHtml = contract.ContractDocumentHtml;
        existing.Status = contract.Status;
        existing.PackageId = contract.PackageId;
        existing.TokenGeneratedAt = contract.TokenGeneratedAt;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> DeleteContract(Guid id)
    {
        var contract = await context.Contracts.FindAsync(id);
        if (contract == null)
            return new HandlerResult(false, "Contrato não encontrado.");

        // Guard deletion integrity: only canceled drafts or contracts can be permanently removed
        if (contract.Status != ContractStatus.Cancelado)
            return new HandlerResult(false, "Apenas contratos cancelados podem ser excluídos.");

        context.Contracts.Remove(contract);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<PaginatedResponse<ContractListItemDto>> QueryContracts(
        string query,
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        var queryable = context.Contracts.Include(c => c.Couple).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
            queryable = queryable.Where(c => c.PatientName.Contains(query));

        var totalItems = await queryable.CountAsync(ct);

        if (totalItems <= 0)
            return new PaginatedResponse<ContractListItemDto>([], totalItems, page, pageSize);

        var rawItems = await queryable
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
                c.CreatedAt,
                c.PatientId,
                c.CoupleId,
                CoupleName = c.Couple != null ? c.Couple.Name : null,
            })
            .ToListAsync(ct);

        var items = rawItems
            .Select(c => new ContractListItemDto(
                c.Id,
                c.CreatedAt.ToString("yyMMdd") + "-" + c.Id.ToString().Substring(0, 4).ToUpper(),
                c.PatientName,
                c.Status,
                c.Type,
                c.Price,
                c.CreatedAt,
                c.PatientId,
                c.CoupleId,
                c.CoupleName
            ))
            .ToList();

        return new PaginatedResponse<ContractListItemDto>(items, totalItems, page, pageSize);
    }

    public async Task<HandlerResult> ConvertToPatient(Guid id)
    {
        var contract = await context.Contracts.FindAsync(id);
        if (contract == null)
            return new HandlerResult(false, "Contrato não encontrado.");

        if (contract.PatientId.HasValue)
            return new HandlerResult(false, "Este contrato já está vinculado a um paciente.");

        if (contract.CoupleId.HasValue)
            return new HandlerResult(
                false,
                "Contrato de casal não pode ser convertido em paciente individual."
            );

        var phone = !string.IsNullOrEmpty(contract.PatientPhone)
            ? OnlyNumbersRegex().Replace(contract.PatientPhone, "")
            : contract.PatientPhone;

        var cpf = !string.IsNullOrEmpty(contract.PatientCpf)
            ? OnlyNumbersRegex().Replace(contract.PatientCpf, "")
            : contract.PatientCpf;

        if (!string.IsNullOrEmpty(cpf))
        {
            var cpfHash = CpfHelper.ComputeHash(cpf);

            var existingContract = await context.Contracts.AnyAsync(c =>
                c.PatientCpfHash == cpfHash && c.Id != contract.Id
            );
            if (existingContract)
                return new HandlerResult(false, "Já existe um contrato com este CPF.");

            var existingPatient = await context.Patients.AnyAsync(p => p.CpfHash == cpfHash);
            if (existingPatient)
                return new HandlerResult(false, "Já existe um paciente com este CPF.");
        }

        var patient = new Patient
        {
            Name = contract.PatientName,
            Cpf = cpf,
            Email = contract.PatientEmail,
            Phone = phone,
            StateOfResidency = contract.PatientState,
            BirthDate = contract.PatientBirthDate,
            PolicySigned = contract.TermsAccepted,
            CpfHash = !string.IsNullOrEmpty(cpf) ? CpfHelper.ComputeHash(cpf) : null,
        };

        // Wrap patient creation and contract association inside a transaction to ensure database consistency
        await using var transaction = await context.Database.BeginTransactionAsync();

        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        contract.PatientId = patient.Id;
        contract.UpdatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();

        await transaction.CommitAsync();
        return new HandlerResult(true);
    }

    public async Task<Contract?> GetContractByPatientId(Guid patientId)
    {
        return await context
            .Contracts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.PatientId == patientId);
    }

    public async Task<Contract?> GetContractByCoupleId(Guid coupleId)
    {
        return await context
            .Contracts.Include(c => c.Couple!)
                .ThenInclude(c => c.Patient1)
            .Include(c => c.Couple!)
                .ThenInclude(c => c.Patient2)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CoupleId == coupleId);
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"[^\d]")]
    private static partial System.Text.RegularExpressions.Regex OnlyNumbersRegex();

    [System.Text.RegularExpressions.GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial System.Text.RegularExpressions.Regex EmailRegex();
}
