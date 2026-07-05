using landing_page_isis.core;
using landing_page_isis.core.Models;
using landing_page_isis.Handlers;
using landing_page_isis.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace landing_page_isis.tests;

public class ContractHandlerTests
{
    private AppDbContext GetDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        Environment.SetEnvironmentVariable("ENCRYPTION_KEY", "test-encryption-key-32-bytes----");

        return context;
    }

    private ContractHandler CreateHandler(AppDbContext context)
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(default(HttpContext));

        var cache = new MemoryCache(new MemoryCacheOptions());

        return new ContractHandler(context, httpContextAccessorMock.Object, cache);
    }

    private static Contract CreateValidContract(string? cpf = null)
    {
        return new Contract
        {
            Id = Guid.NewGuid(),
            PatientName = "Maria Silva",
            PatientCpf = cpf ?? "529.982.247-25",
            PatientEmail = "maria@email.com",
            PatientPhone = "(11) 99999-8888",
            PatientState = "RO",
            PatientBirthDate = new DateOnly(1990, 5, 15),
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
        Assert.Equal("Maria Silva", saved.PatientName);
        Assert.Equal("11999998888", saved.PatientPhone);
        Assert.Equal("52998224725", saved.PatientCpf);
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
        contract.PatientName = "";

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
        contract.PatientEmail = "invalid-email";

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
        contract.PatientPhone = "123";

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
    public async Task CreateContract_ShouldRespectRateLimit()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        for (int i = 0; i < 2; i++)
        {
            var c = CreateValidContract(cpf: null);
            c.PatientCpf = null;
            Assert.True((await handler.CreateContract(c)).Success);
        }

        var blocked = CreateValidContract(cpf: null);
        blocked.PatientCpf = null;
        var result = await handler.CreateContract(blocked);

        Assert.False(result.Success);
        Assert.Contains("Muitas solicitações", result.Message);
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
                PatientName = $"Paciente {i}",
                PatientPhone = "11999999999",
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
            new Contract { Id = Guid.NewGuid(), PatientName = "João Silva", PatientPhone = "11999999999", TermsAccepted = true },
            new Contract { Id = Guid.NewGuid(), PatientName = "Maria Souza", PatientPhone = "11999999999", TermsAccepted = true },
            new Contract { Id = Guid.NewGuid(), PatientName = "Joana Santos", PatientPhone = "11999999999", TermsAccepted = true }
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
            PatientName = "Test",
            PatientPhone = "11999999999",
            TermsAccepted = true,
        });
        await context.SaveChangesAsync();

        var result = await handler.GetContract(id);

        Assert.NotNull(result);
        Assert.Equal("Test", result.PatientName);
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
            PatientName = "Token Test",
            PatientPhone = "11999999999",
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
            PatientName = "Old Name",
            PatientPhone = "11999999999",
            TermsAccepted = true,
            Price = 100,
        });
        await context.SaveChangesAsync();

        var updated = new Contract
        {
            Id = id,
            PatientName = "Old Name",
            PatientPhone = "11999999999",
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
            PatientName = "Ghost",
            PatientPhone = "11999999999",
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
            PatientName = "Accept Test",
            PatientPhone = "11999999999",
            TermsAccepted = true,
            Status = ContractStatus.AguardandoAceitacao,
            AcceptanceToken = "accept-token",
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
        Assert.Equal("Link inválido.", result.Message);
    }

    [Fact]
    public async Task AcceptContract_ShouldReturnFalse_WhenStatusNotAguardando()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        context.Contracts.Add(new Contract
        {
            Id = Guid.NewGuid(),
            PatientName = "Wrong Status",
            PatientPhone = "11999999999",
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
            PatientName = "Expired",
            PatientPhone = "11999999999",
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
            PatientName = "Delete Me",
            PatientPhone = "11999999999",
            TermsAccepted = true,
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
            PatientName = "Convertido Silva",
            PatientCpf = "529.982.247-25",
            PatientEmail = "convertido@email.com",
            PatientPhone = "(11) 91234-5678",
            PatientState = "RO",
            PatientBirthDate = new DateOnly(1988, 3, 20),
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
            PatientName = "Ja Convertido",
            PatientPhone = "11999999999",
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
            PatientName = "No Cpf",
            PatientPhone = "11999999999",
            PatientCpf = null,
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
            PatientName = "Empty Phone Cpf",
            PatientPhone = "",
            PatientCpf = "",
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
}

