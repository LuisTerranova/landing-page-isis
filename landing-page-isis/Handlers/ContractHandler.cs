using FluentValidation;
using landing_page_isis.core;
using landing_page_isis.core.Helpers;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Models;
using landing_page_isis.core.Models.DTOs;
using landing_page_isis.Extensions;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Handlers;

/// <summary>
/// Handles patient/couple service contracts, signature link generation and validation, CPF cryptographic checks, and contract-to-patient conversion.
/// </summary>
public class ContractHandler(
    AppDbContext context,
    IHttpContextAccessor httpContextAccessor,
    IValidator<ContractParticipantInfo> participantValidator
) : IContractHandler
{
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
                PatientName = c.PrimaryPatient.Name,
                c.Status,
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
                c.CreatedAt.ToString("yyMMdd")
                    + "-"
                    + c.Id.ToString().Substring(0, 4).ToUpper(),
                c.PatientName,
                c.Status,
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

        var ip =
            httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!await RateLimiterHelper.CheckAsync($"contract:{ip}", 5, TimeSpan.FromMinutes(1)))
            return new HandlerResult(false, "Muitas tentativas. Tente novamente mais tarde.");

        if (!contract.TermsAccepted)
            return new HandlerResult(false, "É necessário aceitar os termos.");

        Guid? excludePatient1Id = contract.PatientId;
        Guid? excludePatient2Id = null;

        if (contract.CoupleId.HasValue)
        {
            var couple = await context
                .Couples.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contract.CoupleId.Value);
            if (couple != null)
            {
                excludePatient1Id = couple.Patient1Id;
                excludePatient2Id = couple.Patient2Id;
            }
        }

        var primaryResult = await ValidateAndHashParticipant(
            contract.PrimaryPatient,
            contract.Id,
            excludePatient1Id,
            excludePatient2Id,
            isRequired: true,
            "Paciente"
        );
        if (!primaryResult.Success)
            return primaryResult;

        if (
            contract.SecondaryPatient != null
            && !string.IsNullOrWhiteSpace(contract.SecondaryPatient.Name)
        )
        {
            var secondaryResult = await ValidateAndHashParticipant(
                contract.SecondaryPatient,
                contract.Id,
                excludePatient1Id,
                excludePatient2Id,
                isRequired: false,
                "Segundo paciente"
            );
            if (!secondaryResult.Success)
                return secondaryResult;

            if (
                contract.PrimaryPatient.Cpf == contract.SecondaryPatient.Cpf
                && !string.IsNullOrEmpty(contract.PrimaryPatient.Cpf)
            )
                return new HandlerResult(false, "Os dois pacientes devem ser diferentes.");
        }

        contract.CreatedAt = DateTimeOffset.UtcNow;
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();
        return new HandlerResult(true);
    }

    public async Task<HandlerResult> AcceptContract(string token, string? documentHtml = null)
    {
        var ip =
            httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!await RateLimiterHelper.CheckAsync($"accept:{ip}", 5, TimeSpan.FromMinutes(5)))
            return new HandlerResult(false, "Muitas tentativas. Tente novamente mais tarde.");

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

        await using var transaction = await context.Database.BeginTransactionAsync();

        contract.Status = ContractStatus.Ativo;
        contract.AcceptedAt = DateTimeOffset.UtcNow;
        contract.UpdatedAt = DateTimeOffset.UtcNow;

        if (documentHtml != null)
            contract.ContractDocumentHtml = documentHtml;

        await context.SaveChangesAsync();

        await CreateEntitiesFromContract(contract);

        await transaction.CommitAsync();
        return new HandlerResult(true);
    }

    private async Task CreateEntitiesFromContract(Contract contract)
    {
        if (contract.PatientId.HasValue || contract.CoupleId.HasValue)
            return;

        if (
            contract.SecondaryPatient == null
            || string.IsNullOrWhiteSpace(contract.SecondaryPatient.Name)
        )
        {
            // Contrato Individual
            var cpfHash = !string.IsNullOrEmpty(contract.PrimaryPatient.Cpf)
                ? CpfHelper.ComputeHash(CpfValidator.Strip(contract.PrimaryPatient.Cpf))
                : null;

            Patient? existingPatient = null;
            if (cpfHash != null)
            {
                existingPatient = await context.Patients.FirstOrDefaultAsync(p =>
                    p.CpfHash == cpfHash
                );
            }

            if (existingPatient != null)
            {
                // Paciente já existe, apenas vincula e garante assinatura da política
                contract.PatientId = existingPatient.Id;
                existingPatient.PolicySigned = true;
                context.Patients.Update(existingPatient);
            }
            else
            {
                // Cria novo paciente
                var patient = CreatePatientFromParticipant(
                    contract.PrimaryPatient,
                    policySigned: true
                );
                context.Patients.Add(patient);
                await context.SaveChangesAsync();
                contract.PatientId = patient.Id;
            }

            contract.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
        }
        else
        {
            // Contrato de Casal
            var cpfHash1 = !string.IsNullOrEmpty(contract.PrimaryPatient.Cpf)
                ? CpfHelper.ComputeHash(CpfValidator.Strip(contract.PrimaryPatient.Cpf))
                : null;
            var cpfHash2 = !string.IsNullOrEmpty(contract.SecondaryPatient.Cpf)
                ? CpfHelper.ComputeHash(CpfValidator.Strip(contract.SecondaryPatient.Cpf))
                : null;

            Patient? p1 =
                cpfHash1 != null
                    ? await context.Patients.FirstOrDefaultAsync(p => p.CpfHash == cpfHash1)
                    : null;
            Patient? p2 =
                cpfHash2 != null
                    ? await context.Patients.FirstOrDefaultAsync(p => p.CpfHash == cpfHash2)
                    : null;

            if (p1 == null)
            {
                p1 = CreatePatientFromParticipant(contract.PrimaryPatient, policySigned: true);
                context.Patients.Add(p1);
            }
            else
            {
                p1.PolicySigned = true;
                context.Patients.Update(p1);
            }

            if (p2 == null)
            {
                p2 = CreatePatientFromParticipant(contract.SecondaryPatient, policySigned: true);
                context.Patients.Add(p2);
            }
            else
            {
                p2.PolicySigned = true;
                context.Patients.Update(p2);
            }

            await context.SaveChangesAsync();

            // Busca se já existe uma relação de casal entre esses dois pacientes (independente da ordem)
            var couple = await context.Couples.FirstOrDefaultAsync(c =>
                (c.Patient1Id == p1.Id && c.Patient2Id == p2.Id)
                || (c.Patient1Id == p2.Id && c.Patient2Id == p1.Id)
            );

            if (couple == null)
            {
                var coupleName = !string.IsNullOrWhiteSpace(contract.CoupleName)
                    ? contract.CoupleName
                    : $"{p1.Name} & {p2.Name}";

                couple = new Couple
                {
                    Name = coupleName,
                    Patient1Id = p1.Id,
                    Patient2Id = p2.Id,
                    PayerName = contract.PrimaryPatient.Name,
                    PayerCpf = p1.Cpf,
                    PolicySigned = true,
                };
                context.Couples.Add(couple);
                await context.SaveChangesAsync();
            }
            else
            {
                couple.PolicySigned = true;
                context.Couples.Update(couple);
                await context.SaveChangesAsync();
            }

            contract.CoupleId = couple.Id;
            contract.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task<HandlerResult> UpdateContract(Contract contract)
    {
        var existing = await context.Contracts.FindAsync(contract.Id);
        if (existing == null)
            return new HandlerResult(false, "Contrato não encontrado.");

        // Canceled contracts are immutable
        if (existing.Status == ContractStatus.Cancelado)
            return new HandlerResult(false, "Não é possível alterar um contrato cancelado.");

        // Active contracts can only be canceled (no other edits allowed)
        if (existing.Status == ContractStatus.Ativo)
        {
            if (contract.Status != ContractStatus.Cancelado)
                return new HandlerResult(
                    false,
                    "Contrato ativo só pode ser alterado para Cancelado."
                );

            existing.Status = ContractStatus.Cancelado;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
            return new HandlerResult(true);
        }

        // Force transition to Active status to happen only through AcceptContract (initiated by the patient)
        if (contract.Status == ContractStatus.Ativo)
            return new HandlerResult(
                false,
                "Status Ativo só pode ser definido pelo paciente ao aceitar o contrato."
            );

        // Manual mapping is preserved here to prevent SetValues from overwriting uneditable fields with default/null values
        existing.Price = contract.Price;
        existing.InitialAppointments = contract.InitialAppointments;
        existing.PackagePrice = contract.PackagePrice;
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
            queryable = queryable.Where(c => c.PrimaryPatient.Name.Contains(query));

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
                PatientName = c.PrimaryPatient.Name,
                c.Status,
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

        if (!string.IsNullOrEmpty(contract.PrimaryPatient.Cpf))
        {
            var cpf = CpfValidator.Strip(contract.PrimaryPatient.Cpf);
            var cpfHash = CpfHelper.ComputeHash(cpf);

            var existingContract = await context.Contracts.AnyAsync(c =>
                (
                    c.PrimaryPatient.CpfHash == cpfHash
                    || (c.SecondaryPatient != null && c.SecondaryPatient.CpfHash == cpfHash)
                )
                && c.Id != contract.Id
            );
            if (existingContract)
                return new HandlerResult(false, "Já existe um contrato com este CPF.");

            var existingPatient = await context.Patients.AnyAsync(p => p.CpfHash == cpfHash);
            if (existingPatient)
                return new HandlerResult(false, "Já existe um paciente com este CPF.");
        }

        var patient = CreatePatientFromParticipant(
            contract.PrimaryPatient,
            policySigned: contract.TermsAccepted
        );

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

    public async Task<bool> VerifyCpfDigits(Guid contractId, string inputDigits)
    {
        var contract = await context
            .Contracts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == contractId);
        if (contract == null || string.IsNullOrEmpty(contract.PrimaryPatient.Cpf))
            return true;

        var digits = new string(contract.PrimaryPatient.Cpf.Where(char.IsDigit).ToArray());
        var lastThree = digits.Length >= 3 ? digits[^3..] : digits;
        return inputDigits.Trim() == lastThree;
    }

    private async Task<HandlerResult> ValidateAndHashParticipant(
        ContractParticipantInfo participant,
        Guid contractId,
        Guid? excludePatient1Id,
        Guid? excludePatient2Id,
        bool isRequired,
        string participantLabel
    )
    {
        var prefix = participantLabel == "Paciente" ? "" : $"{participantLabel}: ";

        if (isRequired && string.IsNullOrWhiteSpace(participant.Name))
            return new HandlerResult(false, $"{prefix}Nome é obrigatório.");

        if (isRequired && string.IsNullOrWhiteSpace(participant.Phone))
            return new HandlerResult(false, $"{prefix}Telefone é obrigatório.");

        if (!isRequired && string.IsNullOrWhiteSpace(participant.Name))
            return new HandlerResult(true);

        var validation = await participantValidator.ValidateAsync(participant);
        if (!validation.IsValid)
            return new HandlerResult(false, $"{prefix}{validation.Errors.First().ErrorMessage}");

        if (!string.IsNullOrEmpty(participant.Phone))
            participant.Phone = CpfValidator.Strip(participant.Phone);

        if (!string.IsNullOrEmpty(participant.Cpf))
        {
            participant.Cpf = CpfValidator.Strip(participant.Cpf);
            var cpfHash = CpfHelper.ComputeHash(participant.Cpf);

            var existingInContracts = await context.Contracts.AnyAsync(c =>
                c.Id != contractId
                && (
                    c.PrimaryPatient.CpfHash == cpfHash
                    || (c.SecondaryPatient != null && c.SecondaryPatient.CpfHash == cpfHash)
                )
                && c.Status != ContractStatus.Cancelado
            );

            if (existingInContracts)
                return new HandlerResult(
                    false,
                    participantLabel == "Paciente"
                        ? "Já existe um cadastro com este CPF."
                        : $"Já existe um cadastro com o CPF do {participantLabel.ToLower()}."
                );

            var existingInPatients = await context.Patients.AnyAsync(p =>
                p.CpfHash == cpfHash && p.Id != excludePatient1Id && p.Id != excludePatient2Id
            );

            if (existingInPatients)
                return new HandlerResult(
                    false,
                    participantLabel == "Paciente"
                        ? "Já existe um cadastro com este CPF."
                        : $"Já existe um cadastro com o CPF do {participantLabel.ToLower()}."
                );

            participant.CpfHash = cpfHash;
        }

        return new HandlerResult(true);
    }

    private Patient CreatePatientFromParticipant(
        ContractParticipantInfo participant,
        bool policySigned
    )
    {
        var cpf = !string.IsNullOrEmpty(participant.Cpf)
            ? CpfValidator.Strip(participant.Cpf)
            : participant.Cpf;

        return new Patient
        {
            Name = participant.Name,
            Cpf = cpf,
            Email = participant.Email,
            Phone = !string.IsNullOrEmpty(participant.Phone)
                ? CpfValidator.Strip(participant.Phone)
                : participant.Phone,
            StateOfResidency = participant.State,
            BirthDate = participant.BirthDate,
            PolicySigned = policySigned,
            CpfHash = !string.IsNullOrEmpty(cpf) ? CpfHelper.ComputeHash(cpf) : null,
        };
    }
}
