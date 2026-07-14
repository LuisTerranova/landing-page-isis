using landing_page_isis.core;
using landing_page_isis.core.Models;
using landing_page_isis.Handlers;
using landing_page_isis.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace landing_page_isis.tests;

public class ContractHandlerTests
{
    private AppDbContext GetDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        Environment.SetEnvironmentVariable("ENCRYPTION_KEY", "test-encryption-key-32-bytes----");
        Environment.SetEnvironmentVariable("CPF_HASH_PEPPER", "test-pepper-secret");
        landing_page_isis.Extensions.RateLimiterHelper.Reset();

        return context;
    }

    private static ContractHandler CreateHandler(AppDbContext context)
    {
        var httpAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor();
        return new ContractHandler(context, httpAccessor);
    }

    private static Contract CreateValidContract(string? cpf = null)
    {
        return new Contract
        {
            Id = Guid.NewGuid(),
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "Maria Silva",
                Cpf = cpf ?? "529.982.247-25",
                Email = "maria@email.com",
                Phone = "(11) 99999-8888",
                State = "RO",
                BirthDate = new DateOnly(1990, 5, 15),
            },
            TermsAccepted = true,
            Status = ContractStatus.Rascunho,
        };
    }

    [Fact]
    public async Task CreateContract_ShouldReturnTrue_WhenValid()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);
        var contract = CreateValidContract();

        var result = await handler.CreateContract(contract);

        Assert.True(result.Success);
        var saved = await context.Contracts.FindAsync(contract.Id);
        Assert.NotNull(saved);
        Assert.Equal("Maria Silva", saved.PrimaryPatient.Name);
        Assert.Equal("11999998888", saved.PrimaryPatient.Phone);
        Assert.Equal("52998224725", saved.PrimaryPatient.Cpf);
    }

    [Fact]
    public async Task CreateContract_ShouldReturnFalse_WhenNull()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var result = await handler.CreateContract(null);

        Assert.False(result.Success);
        Assert.Equal("Dados inválidos.", result.Message);
    }

    [Fact]
    public async Task CreateContract_ShouldReturnFalse_WhenNameIsEmpty()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);
        var contract = CreateValidContract();
        contract.PrimaryPatient.Name = "";

        var result = await handler.CreateContract(contract);

        Assert.False(result.Success);
        Assert.Equal("Nome é obrigatório.", result.Message);
    }

    [Fact]
    public async Task CreateContract_ShouldReturnFalse_WhenEmailIsInvalid()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);
        var contract = CreateValidContract();
        contract.PrimaryPatient.Email = "invalid-email";

        var result = await handler.CreateContract(contract);

        Assert.False(result.Success);
        Assert.Equal("E-mail inválido.", result.Message);
    }

    [Fact]
    public async Task CreateContract_ShouldReturnFalse_WhenPhoneIsInvalid()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);
        var contract = CreateValidContract();
        contract.PrimaryPatient.Phone = "123";

        var result = await handler.CreateContract(contract);

        Assert.False(result.Success);
        Assert.Equal("Telefone inválido. Deve ter 10 ou 11 dígitos.", result.Message);
    }

    [Fact]
    public async Task CreateContract_ShouldReturnFalse_WhenCpfIsInvalid()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);
        var contract = CreateValidContract("123.456.789-00");

        var result = await handler.CreateContract(contract);

        Assert.False(result.Success);
        Assert.Equal("CPF inválido.", result.Message);
    }

    [Fact]
    public async Task CreateContract_ShouldReturnFalse_WhenCpfHasRepeatedDigits()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);
        var contract = CreateValidContract("111.111.111-11");

        var result = await handler.CreateContract(contract);

        Assert.False(result.Success);
        Assert.Equal("CPF inválido.", result.Message);
    }

    [Fact]
    public async Task CreateContract_ShouldReturnFalse_WhenTermsNotAccepted()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);
        var contract = CreateValidContract();
        contract.TermsAccepted = false;

        var result = await handler.CreateContract(contract);

        Assert.False(result.Success);
        Assert.Equal("É necessário aceitar os termos.", result.Message);
    }

    [Fact]
    public async Task CreateContract_ShouldReturnFalse_WhenStateIsInvalid()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);
        var contract = CreateValidContract();
        contract.PrimaryPatient.State = "XX";

        var result = await handler.CreateContract(contract);

        Assert.False(result.Success);
        Assert.Equal("Estado (UF) inválido.", result.Message);
    }

    [Fact]
    public async Task CreateContract_ShouldReturnFalse_WhenBirthDateIsFuture()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);
        var contract = CreateValidContract();
        contract.PrimaryPatient.BirthDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        var result = await handler.CreateContract(contract);

        Assert.False(result.Success);
        Assert.Equal("Data de nascimento inválida.", result.Message);
    }

    [Fact]
    public async Task CreateContract_ShouldReturnFalse_WhenCpfAlreadyExists()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var first = CreateValidContract("529.982.247-25");
        await handler.CreateContract(first);

        var duplicate = CreateValidContract("529.982.247-25");
        var result = await handler.CreateContract(duplicate);

        Assert.False(result.Success);
        Assert.Equal("Já existe um cadastro com este CPF.", result.Message);
    }

    [Fact]
    public async Task GetContracts_ShouldReturnPaginatedList()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        for (int i = 0; i < 5; i++)
        {
            context.Contracts.Add(new Contract
            {
                Id = Guid.NewGuid(),
                PrimaryPatient = new ContractParticipantInfo
                {
                    Name = $"Paciente {i}",
                    Phone = "11999999999"
                },
                TermsAccepted = true,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-i),
            });
        }
        await context.SaveChangesAsync();

        var result = await handler.GetContracts(0, 3, CancellationToken.None);

        Assert.Equal(5, result.TotalItems);
        Assert.Equal(3, result.Items.Count());
    }

    [Fact]
    public async Task GetContracts_ShouldReturnEmpty_WhenNone()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var result = await handler.GetContracts(0, 10, CancellationToken.None);

        Assert.Equal(0, result.TotalItems);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task QueryContracts_ShouldFilterByName()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        context.Contracts.AddRange(
            new Contract { Id = Guid.NewGuid(), PrimaryPatient = new ContractParticipantInfo { Name = "João Silva", Phone = "11999999999" }, TermsAccepted = true },
            new Contract { Id = Guid.NewGuid(), PrimaryPatient = new ContractParticipantInfo { Name = "Maria Souza", Phone = "11999999999" }, TermsAccepted = true },
            new Contract { Id = Guid.NewGuid(), PrimaryPatient = new ContractParticipantInfo { Name = "Joana Santos", Phone = "11999999999" }, TermsAccepted = true }
        );
        await context.SaveChangesAsync();

        var result = await handler.QueryContracts("João", 0, 10, CancellationToken.None);

        Assert.Equal(1, result.TotalItems);
        Assert.Contains(result.Items, c => c.PatientName == "João Silva");
    }

    [Fact]
    public async Task GetContract_ShouldReturnContract_WhenExists()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var id = Guid.NewGuid();
        context.Contracts.Add(new Contract
        {
            Id = id,
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "Test",
                Phone = "11999999999"
            },
            TermsAccepted = true,
        });
        await context.SaveChangesAsync();

        var result = await handler.GetContract(id);

        Assert.NotNull(result);
        Assert.Equal("Test", result.PrimaryPatient.Name);
    }

    [Fact]
    public async Task GetContract_ShouldReturnNull_WhenNotFound()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var result = await handler.GetContract(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetContractByToken_ShouldReturnContract_WhenExists()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var id = Guid.NewGuid();
        context.Contracts.Add(new Contract
        {
            Id = id,
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "Token Test",
                Phone = "11999999999"
            },
            TermsAccepted = true,
            AcceptanceToken = "abc-123",
        });
        await context.SaveChangesAsync();

        var result = await handler.GetContractByToken("abc-123");

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task GetContractByToken_ShouldReturnNull_WhenNotFound()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var result = await handler.GetContractByToken("non-existent");

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateContract_ShouldReturnTrue_WhenValid()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var id = Guid.NewGuid();
        context.Contracts.Add(new Contract
        {
            Id = id,
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "Old Name",
                Phone = "11999999999"
            },
            TermsAccepted = true,
            Price = 100,
        });
        await context.SaveChangesAsync();

        var updated = new Contract
        {
            Id = id,
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "Old Name",
                Phone = "11999999999"
            },
            TermsAccepted = true,
            Price = 150,
        };

        var result = await handler.UpdateContract(updated);

        Assert.True(result.Success);
        var saved = await context.Contracts.FindAsync(id);
        Assert.NotNull(saved);
        Assert.Equal(150, saved.Price);
    }

    [Fact]
    public async Task UpdateContract_ShouldReturnFalse_WhenNotFound()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var result = await handler.UpdateContract(new Contract
        {
            Id = Guid.NewGuid(),
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "Ghost",
                Phone = "11999999999"
            },
            TermsAccepted = true,
        });

        Assert.False(result.Success);
        Assert.Equal("Contrato não encontrado.", result.Message);
    }

    [Fact]
    public async Task AcceptContract_ShouldSetStatusToAtivo()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var id = Guid.NewGuid();
        context.Contracts.Add(new Contract
        {
            Id = id,
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "Accept Test",
                Phone = "11999999999"
            },
            TermsAccepted = true,
            Status = ContractStatus.AguardandoAceitacao,
            AcceptanceToken = "accept-token",
            TokenGeneratedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync();

        var result = await handler.AcceptContract("accept-token");

        Assert.True(result.Success);
        var saved = await context.Contracts.FindAsync(id);
        Assert.NotNull(saved);
        Assert.Equal(ContractStatus.Ativo, saved.Status);
        Assert.NotNull(saved.AcceptedAt);
    }

    [Fact]
    public async Task AcceptContract_ShouldReturnFalse_WhenTokenInvalid()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var result = await handler.AcceptContract("invalid-token");

        Assert.False(result.Success);
        Assert.Equal("Link inválido ou expirado.", result.Message);
    }

    [Fact]
    public async Task AcceptContract_ShouldReturnFalse_WhenStatusNotAguardando()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        context.Contracts.Add(new Contract
        {
            Id = Guid.NewGuid(),
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "Wrong Status",
                Phone = "11999999999"
            },
            TermsAccepted = true,
            Status = ContractStatus.Rascunho,
            AcceptanceToken = "rascunho-token",
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync();

        var result = await handler.AcceptContract("rascunho-token");

        Assert.False(result.Success);
        Assert.Equal("Contrato não está aguardando aceitação.", result.Message);
    }

    [Fact]
    public async Task AcceptContract_ShouldReturnFalse_WhenLinkExpired()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        context.Contracts.Add(new Contract
        {
            Id = Guid.NewGuid(),
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "Expired",
                Phone = "11999999999"
            },
            TermsAccepted = true,
            Status = ContractStatus.AguardandoAceitacao,
            AcceptanceToken = "expired-token",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-3),
        });
        await context.SaveChangesAsync();

        var result = await handler.AcceptContract("expired-token");

        Assert.False(result.Success);
        Assert.Contains("expirado", result.Message);
    }

    [Fact]
    public async Task DeleteContract_ShouldReturnTrue_WhenExists()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var id = Guid.NewGuid();
        context.Contracts.Add(new Contract
        {
            Id = id,
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "Delete Me",
                Phone = "11999999999"
            },
            TermsAccepted = true,
            Status = ContractStatus.Cancelado,
        });
        await context.SaveChangesAsync();

        var result = await handler.DeleteContract(id);

        Assert.True(result.Success);
        Assert.Null(await context.Contracts.FindAsync(id));
    }

    [Fact]
    public async Task DeleteContract_ShouldReturnFalse_WhenNotFound()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var result = await handler.DeleteContract(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("Contrato não encontrado.", result.Message);
    }

    [Fact]
    public async Task ConvertToPatient_ShouldCreatePatient_WhenContractExists()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var contractId = Guid.NewGuid();
        context.Contracts.Add(new Contract
        {
            Id = contractId,
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "Convertido Silva",
                Cpf = "529.982.247-25",
                Email = "convertido@email.com",
                Phone = "(11) 91234-5678",
                State = "RO",
                BirthDate = new DateOnly(1988, 3, 20),
            },
            TermsAccepted = true,
        });
        await context.SaveChangesAsync();

        var result = await handler.ConvertToPatient(contractId);

        Assert.True(result.Success);
        var patient = await context.Patients.FirstOrDefaultAsync(p => p.Name == "Convertido Silva");
        Assert.NotNull(patient);
        Assert.Equal("52998224725", patient.Cpf);
        Assert.Equal("convertido@email.com", patient.Email);
        Assert.Equal("RO", patient.StateOfResidency);
        Assert.Equal(new DateOnly(1988, 3, 20), patient.BirthDate);
        Assert.True(patient.PolicySigned);
    }

    [Fact]
    public async Task ConvertToPatient_ShouldReturnFalse_WhenContractNotFound()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var result = await handler.ConvertToPatient(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("Contrato não encontrado.", result.Message);
    }

    [Fact]
    public async Task ConvertToPatient_ShouldReturnFalse_WhenContractAlreadyLinkedToPatient()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var contractId = Guid.NewGuid();
        context.Contracts.Add(new Contract
        {
            Id = contractId,
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "Ja Convertido",
                Phone = "11999999999"
            },
            TermsAccepted = true,
            PatientId = Guid.NewGuid() // already linked
        });
        await context.SaveChangesAsync();

        var result = await handler.ConvertToPatient(contractId);

        Assert.False(result.Success);
        Assert.Equal("Este contrato já está vinculado a um paciente.", result.Message);
    }

    [Fact]
    public async Task ConvertToPatient_ShouldCreatePatient_WhenCpfIsNullAndPhoneIsValid()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var contractId = Guid.NewGuid();
        context.Contracts.Add(new Contract
        {
            Id = contractId,
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "No Cpf",
                Phone = "11999999999",
                Cpf = null
            },
            TermsAccepted = true
        });
        await context.SaveChangesAsync();

        var result = await handler.ConvertToPatient(contractId);

        Assert.True(result.Success);
        var patient = await context.Patients.FirstOrDefaultAsync(p => p.Name == "No Cpf");
        Assert.NotNull(patient);
        Assert.Equal("11999999999", patient.Phone);
        Assert.Null(patient.Cpf);
    }

    [Fact]
    public async Task ConvertToPatient_ShouldCreatePatient_WhenPhoneAndCpfAreEmpty()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var contractId = Guid.NewGuid();
        context.Contracts.Add(new Contract
        {
            Id = contractId,
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "Empty Phone Cpf",
                Phone = "",
                Cpf = ""
            },
            TermsAccepted = true
        });
        await context.SaveChangesAsync();

        var result = await handler.ConvertToPatient(contractId);

        Assert.True(result.Success);
        var patient = await context.Patients.FirstOrDefaultAsync(p => p.Name == "Empty Phone Cpf");
        Assert.NotNull(patient);
        Assert.Equal("", patient.Phone);
        Assert.Equal("", patient.Cpf);
    }

    [Fact]
    public async Task UpdateContract_ShouldReturnFalse_WhenContractIsAtivo()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var id = Guid.NewGuid();
        context.Contracts.Add(new Contract
        {
            Id = id,
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "Active Contract",
                Phone = "11999999999"
            },
            TermsAccepted = true,
            Status = ContractStatus.Ativo,
            Price = 100,
        });
        await context.SaveChangesAsync();

        var result = await handler.UpdateContract(new Contract
        {
            Id = id,
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "Active Contract",
                Phone = "11999999999"
            },
            TermsAccepted = true,
            Price = 150,
            Status = ContractStatus.Ativo,
        });

        Assert.False(result.Success);
        Assert.Equal("Contrato ativo só pode ser alterado para Cancelado.", result.Message);
    }

    [Fact]
    public async Task UpdateContract_ShouldCancel_WhenChangingFromAtivo()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var id = Guid.NewGuid();
        context.Contracts.Add(new Contract
        {
            Id = id,
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "To Cancel",
                Phone = "11999999999"
            },
            TermsAccepted = true,
            Status = ContractStatus.Ativo,
        });
        await context.SaveChangesAsync();

        var result = await handler.UpdateContract(new Contract
        {
            Id = id,
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "To Cancel",
                Phone = "11999999999"
            },
            TermsAccepted = true,
            Status = ContractStatus.Cancelado,
        });

        Assert.True(result.Success);
        var saved = await context.Contracts.FindAsync(id);
        Assert.NotNull(saved);
        Assert.Equal(ContractStatus.Cancelado, saved.Status);
    }

    [Fact]
    public async Task UpdateContract_ShouldReturnFalse_WhenContractIsCancelado()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var id = Guid.NewGuid();
        context.Contracts.Add(new Contract
        {
            Id = id,
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "Cancelled",
                Phone = "11999999999"
            },
            TermsAccepted = true,
            Status = ContractStatus.Cancelado,
        });
        await context.SaveChangesAsync();

        var result = await handler.UpdateContract(new Contract
        {
            Id = id,
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "Cancelled",
                Phone = "11999999999"
            },
            TermsAccepted = true,
            Status = ContractStatus.Cancelado,
        });

        Assert.False(result.Success);
        Assert.Equal("Não é possível alterar um contrato cancelado.", result.Message);
    }

    [Fact]
    public async Task AcceptContract_ShouldReturnFalse_WhenTokenGeneratedLinkExpired()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        context.Contracts.Add(new Contract
        {
            Id = Guid.NewGuid(),
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "Expired Token",
                Phone = "11999999999"
            },
            TermsAccepted = true,
            Status = ContractStatus.AguardandoAceitacao,
            AcceptanceToken = "expired-generated",
            TokenGeneratedAt = DateTimeOffset.UtcNow.AddDays(-3),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync();

        var result = await handler.AcceptContract("expired-generated");

        Assert.False(result.Success);
        Assert.Contains("expirado", result.Message);
    }

    [Fact]
    public async Task ConvertToPatient_ShouldReturnFalse_WhenCpfAlreadyExistsInPatient()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var pepper = Environment.GetEnvironmentVariable("CPF_HASH_PEPPER")!;
        var cpfData = System.Text.Encoding.UTF8.GetBytes("52998224725:" + pepper);
        var cpfHash = System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(pepper), cpfData);
        var cpfHashHex = Convert.ToHexStringLower(cpfHash);

        context.Patients.Add(new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Existing Patient",
            Phone = "11999999999",
            Cpf = "529.982.247-25",
            CpfHash = cpfHashHex,
        });
        await context.SaveChangesAsync();

        var contractId = Guid.NewGuid();
        context.Contracts.Add(new Contract
        {
            Id = contractId,
            PrimaryPatient = new ContractParticipantInfo
            {
                Name = "Dup Cpf",
                Cpf = "529.982.247-25",
                Phone = "11999999999"
            },
            TermsAccepted = true,
        });
        await context.SaveChangesAsync();

        var result = await handler.ConvertToPatient(contractId);

        Assert.False(result.Success);
        Assert.Equal("Já existe um paciente com este CPF.", result.Message);
    }

    [Fact]
    public async Task CreateContract_ShouldSucceed_WhenPatientIdIsSpecifiedAndExists()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var patientId = Guid.NewGuid();
        var pepper = Environment.GetEnvironmentVariable("CPF_HASH_PEPPER")!;
        var cpfData = System.Text.Encoding.UTF8.GetBytes("52998224725:" + pepper);
        var cpfHash = System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(pepper), cpfData);
        var cpfHashHex = Convert.ToHexStringLower(cpfHash);

        context.Patients.Add(new Patient
        {
            Id = patientId,
            Name = "Existing Patient",
            Phone = "11999999999",
            Cpf = "529.982.247-25",
            CpfHash = cpfHashHex,
        });
        await context.SaveChangesAsync();

        var contract = CreateValidContract("529.982.247-25");
        contract.PatientId = patientId;

        var result = await handler.CreateContract(contract);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateContract_ShouldFail_WhenPartner2EmailIsInvalid()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);
        var contract = CreateValidContract();
        contract.SecondaryPatient = new ContractParticipantInfo { Name = "Partner Two" };
        contract.SecondaryPatient.Email = "invalid-email";

        var result = await handler.CreateContract(contract);

        Assert.False(result.Success);
        Assert.Equal("Segundo paciente: E-mail inválido.", result.Message);
    }

    [Fact]
    public async Task CreateContract_ShouldFail_WhenPartner2PhoneIsInvalid()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);
        var contract = CreateValidContract();
        contract.SecondaryPatient = new ContractParticipantInfo { Name = "Partner Two" };
        contract.SecondaryPatient.Phone = "123";

        var result = await handler.CreateContract(contract);

        Assert.False(result.Success);
        Assert.Equal("Segundo paciente: Telefone inválido. Deve ter 10 ou 11 dígitos.", result.Message);
    }

    [Fact]
    public async Task CreateContract_ShouldFail_WhenPartner2CpfIsInvalid()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);
        var contract = CreateValidContract();
        contract.SecondaryPatient = new ContractParticipantInfo { Name = "Partner Two" };
        contract.SecondaryPatient.Cpf = "123";

        var result = await handler.CreateContract(contract);

        Assert.False(result.Success);
        Assert.Equal("Segundo paciente: CPF inválido.", result.Message);
    }

    [Fact]
    public async Task CreateContract_ShouldFail_WhenPartnersHaveSameCpf()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);
        var contract = CreateValidContract("529.982.247-25");
        contract.SecondaryPatient = new ContractParticipantInfo { Name = "Partner Two" };
        contract.SecondaryPatient.Cpf = "529.982.247-25";

        var result = await handler.CreateContract(contract);

        Assert.False(result.Success);
        Assert.Equal("Os dois pacientes devem ser diferentes.", result.Message);
    }
}

