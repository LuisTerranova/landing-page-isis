using landing_page_isis.core;
using landing_page_isis.core.Models;
using landing_page_isis.Handlers;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.tests;

public class CoupleHandlerTests
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

    private CoupleHandler GetHandler(AppDbContext context)
    {
        return new CoupleHandler(context, new landing_page_isis.core.Validators.CoupleValidator());
    }

    [Fact]
    public async Task GetCouples_ShouldReturnPaginatedList()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var p1 = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Patient 1",
            Phone = "11999999999",
            Email = "p1@test.com",
        };
        var p2 = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Patient 2",
            Phone = "11999999999",
            Email = "p2@test.com",
        };
        context.Patients.AddRange(p1, p2);

        for (int i = 0; i < 5; i++)
        {
            context.Couples.Add(
                new Couple
                {
                    Id = Guid.NewGuid(),
                    Name = $"Couple {i}",
                    Patient1Id = p1.Id,
                    Patient2Id = p2.Id,
                    Patient1 = p1,
                    Patient2 = p2,
                }
            );
        }
        await context.SaveChangesAsync();

        var result = await handler.GetCouples(0, 3, CancellationToken.None);

        Assert.Equal(5, result.TotalItems);
        Assert.Equal(3, result.Items.Count());
    }

    [Fact]
    public async Task GetCouple_ShouldReturnCouple_WhenExists()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var p1 = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Patient 1",
            Phone = "11999999999",
        };
        var p2 = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Patient 2",
            Phone = "11999999999",
        };
        context.Patients.AddRange(p1, p2);

        var coupleId = Guid.NewGuid();
        var couple = new Couple
        {
            Id = coupleId,
            Name = "Love Birds",
            Patient1Id = p1.Id,
            Patient2Id = p2.Id,
            Patient1 = p1,
            Patient2 = p2,
        };
        context.Couples.Add(couple);
        await context.SaveChangesAsync();

        var result = await handler.GetCouple(coupleId);

        Assert.NotNull(result);
        Assert.Equal(coupleId, result.Id);
        Assert.Equal("Love Birds", result.Name);
    }

    [Fact]
    public async Task CreateCouple_ShouldReturnFalse_WhenNameIsEmpty()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var couple = new Couple
        {
            Name = "",
            Patient1Id = Guid.NewGuid(),
            Patient2Id = Guid.NewGuid(),
        };

        var result = await handler.CreateCouple(couple);

        Assert.False(result.Success);
        Assert.Equal("Nome do casal é obrigatório.", result.Message);
    }

    [Fact]
    public async Task CreateCouple_ShouldReturnFalse_WhenNameTooLong()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var couple = new Couple
        {
            Name = new string('A', 151),
            Patient1Id = Guid.NewGuid(),
            Patient2Id = Guid.NewGuid(),
        };

        var result = await handler.CreateCouple(couple);

        Assert.False(result.Success);
        Assert.Equal("Nome do casal deve ter no máximo 150 caracteres.", result.Message);
    }

    [Fact]
    public async Task CreateCouple_ShouldReturnFalse_WhenPatientsAreSame()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var patientId = Guid.NewGuid();
        var couple = new Couple
        {
            Name = "Couple Same",
            Patient1Id = patientId,
            Patient2Id = patientId,
        };

        var result = await handler.CreateCouple(couple);

        Assert.False(result.Success);
        Assert.Equal("Os dois pacientes devem ser diferentes.", result.Message);
    }

    [Fact]
    public async Task CreateCouple_ShouldReturnFalse_WhenPatientAlreadyInAnotherCouple()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();

        context.Couples.Add(
            new Couple
            {
                Name = "First Couple",
                Patient1Id = p1,
                Patient2Id = p2,
            }
        );
        await context.SaveChangesAsync();

        var newCouple = new Couple
        {
            Name = "Conflict Couple",
            Patient1Id = p1,
            Patient2Id = p3,
        };

        var result = await handler.CreateCouple(newCouple);

        Assert.False(result.Success);
        Assert.Equal("Um dos pacientes já pertence a outro casal.", result.Message);
    }

    [Fact]
    public async Task CreateCouple_ShouldReturnFalse_WhenPayerCpfIsInvalid()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var couple = new Couple
        {
            Name = "Couple Cpf Test",
            Patient1Id = Guid.NewGuid(),
            Patient2Id = Guid.NewGuid(),
            PayerCpf = "123",
        };

        var result = await handler.CreateCouple(couple);

        Assert.False(result.Success);
        Assert.Equal("CPF do pagador inválido. Deve ter 11 dígitos.", result.Message);
    }

    [Fact]
    public async Task CreateCouple_ShouldReturnTrue_WhenValidAndFormatCpf()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var couple = new Couple
        {
            Name = "Couple Cpf Test",
            Patient1Id = Guid.NewGuid(),
            Patient2Id = Guid.NewGuid(),
            PayerCpf = "123.456.789-01",
        };

        var result = await handler.CreateCouple(couple);

        Assert.True(result.Success);
        var saved = await context.Couples.FindAsync(couple.Id);
        Assert.NotNull(saved);
        Assert.Equal("12345678901", saved.PayerCpf);
    }

    [Fact]
    public async Task UpdateCouple_ShouldReturnFalse_WhenNotFound()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var couple = new Couple { Id = Guid.NewGuid(), Name = "Couple" };

        var result = await handler.UpdateCouple(couple);

        Assert.False(result.Success);
        Assert.Equal("Casal não encontrado.", result.Message);
    }

    [Fact]
    public async Task UpdateCouple_ShouldReturnFalse_WhenNameIsEmpty()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var couple = new Couple { Name = "Original Name" };
        context.Couples.Add(couple);
        await context.SaveChangesAsync();

        couple.Name = "";
        var result = await handler.UpdateCouple(couple);

        Assert.False(result.Success);
        Assert.Equal("Nome do casal é obrigatório.", result.Message);
    }

    [Fact]
    public async Task UpdateCouple_ShouldReturnFalse_WhenNameTooLong()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var couple = new Couple { Name = "Original Name" };
        context.Couples.Add(couple);
        await context.SaveChangesAsync();

        couple.Name = new string('A', 151);
        var result = await handler.UpdateCouple(couple);

        Assert.False(result.Success);
        Assert.Equal("Nome do casal deve ter no máximo 150 caracteres.", result.Message);
    }

    [Fact]
    public async Task UpdateCouple_ShouldReturnFalse_WhenPayerCpfIsInvalid()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var couple = new Couple
        {
            Name = "Original Name",
            Patient1Id = Guid.NewGuid(),
            Patient2Id = Guid.NewGuid(),
        };
        context.Couples.Add(couple);
        await context.SaveChangesAsync();

        couple.PayerCpf = "123";
        var result = await handler.UpdateCouple(couple);

        Assert.False(result.Success);
        Assert.Equal("CPF do pagador inválido. Deve ter 11 dígitos.", result.Message);
    }

    [Fact]
    public async Task UpdateCouple_ShouldReturnTrue_WhenValidAndFormatCpf()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var couple = new Couple
        {
            Name = "Original Name",
            Patient1Id = Guid.NewGuid(),
            Patient2Id = Guid.NewGuid(),
        };
        context.Couples.Add(couple);
        await context.SaveChangesAsync();

        couple.Name = "Updated Name";
        couple.PayerCpf = "123.456.789-01";
        var result = await handler.UpdateCouple(couple);

        Assert.True(result.Success);
        var saved = await context.Couples.FindAsync(couple.Id);
        Assert.NotNull(saved);
        Assert.Equal("Updated Name", saved.Name);
        Assert.Equal("12345678901", saved.PayerCpf);
    }

    [Fact]
    public async Task UpdateCouple_ShouldReturnFalse_WhenPatientsAreSame()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var couple = new Couple
        {
            Name = "Original Name",
            Patient1Id = Guid.NewGuid(),
            Patient2Id = Guid.NewGuid(),
        };
        context.Couples.Add(couple);
        await context.SaveChangesAsync();

        var sameId = Guid.NewGuid();
        couple.Patient1Id = sameId;
        couple.Patient2Id = sameId;

        var result = await handler.UpdateCouple(couple);

        Assert.False(result.Success);
        Assert.Equal("Os dois pacientes devem ser diferentes.", result.Message);
    }

    [Fact]
    public async Task UpdateCouple_ShouldReturnFalse_WhenPatientAlreadyInAnotherCouple()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();

        // Existing couple
        context.Couples.Add(
            new Couple
            {
                Name = "First Couple",
                Patient1Id = p1,
                Patient2Id = p2,
            }
        );

        // Couple to update
        var couple = new Couple
        {
            Name = "Second Couple",
            Patient1Id = Guid.NewGuid(),
            Patient2Id = p3,
        };
        context.Couples.Add(couple);
        await context.SaveChangesAsync();

        // Update second couple to use p1
        couple.Patient1Id = p1;

        var result = await handler.UpdateCouple(couple);

        Assert.False(result.Success);
        Assert.Equal("Um dos pacientes já pertence a outro casal.", result.Message);
    }

    [Fact]
    public async Task DeleteCouple_ShouldReturnFalse_WhenNotFound()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var result = await handler.DeleteCouple(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("Casal não encontrado.", result.Message);
    }

    [Fact]
    public async Task DeleteCouple_ShouldReturnTrue_WhenExists()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var couple = new Couple { Name = "Delete Me" };
        context.Couples.Add(couple);
        await context.SaveChangesAsync();

        var result = await handler.DeleteCouple(couple.Id);

        Assert.True(result.Success);
        var saved = await context.Couples.FindAsync(couple.Id);
        Assert.Null(saved);
    }

    [Fact]
    public async Task QueryCouples_ShouldReturnFilteredCouples()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var p1 = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Patient 1",
            Phone = "11999999999",
            Email = "p1@test.com",
        };
        var p2 = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Patient 2",
            Phone = "11999999999",
            Email = "p2@test.com",
        };
        context.Patients.AddRange(p1, p2);

        context.Couples.AddRange(
            new Couple
            {
                Name = "Adam & Eve",
                Patient1 = p1,
                Patient2 = p2,
            },
            new Couple
            {
                Name = "Romeo & Juliet",
                Patient1 = p1,
                Patient2 = p2,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.QueryCouples("Romeo", 0, 10, CancellationToken.None);

        Assert.Equal(1, result.TotalItems);
        Assert.Equal("Romeo & Juliet", result.Items.First().Name);
    }

    [Fact]
    public async Task GetCoupleByPatientId_ShouldReturnCouple_WhenPatientIsMember()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var p1 = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Patient 1",
            Phone = "11999999999",
        };
        var p2 = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Patient 2",
            Phone = "11999999999",
        };
        context.Patients.AddRange(p1, p2);

        var couple = new Couple
        {
            Name = "Test Couple",
            Patient1Id = p1.Id,
            Patient2Id = p2.Id,
            Patient1 = p1,
            Patient2 = p2,
        };
        context.Couples.Add(couple);
        await context.SaveChangesAsync();

        var result = await handler.GetCoupleByPatientId(p1.Id);

        Assert.NotNull(result);
        Assert.Equal(couple.Id, result.Id);
    }

    [Fact]
    public async Task GetAllCouples_ShouldReturnAllCouples()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var p1 = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Patient 1",
            Phone = "11999999999",
            Email = "p1@test.com",
        };
        var p2 = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Patient 2",
            Phone = "11999999999",
            Email = "p2@test.com",
        };
        context.Patients.AddRange(p1, p2);

        context.Couples.AddRange(
            new Couple
            {
                Name = "Couple A",
                Patient1 = p1,
                Patient2 = p2,
            },
            new Couple
            {
                Name = "Couple B",
                Patient1 = p1,
                Patient2 = p2,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.GetAllCouples(CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    private static Couple AddCouple(
        AppDbContext context,
        string name = "Casal Teste",
        bool active = true,
        bool patientsActive = true
    )
    {
        var p1 = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Parceiro 1",
            Phone = "11999999999",
            IsActive = patientsActive,
        };
        var p2 = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Parceiro 2",
            Phone = "11999999998",
            IsActive = patientsActive,
        };
        context.Patients.AddRange(p1, p2);

        var couple = new Couple
        {
            Id = Guid.NewGuid(),
            Name = name,
            Patient1Id = p1.Id,
            Patient2Id = p2.Id,
            Patient1 = p1,
            Patient2 = p2,
            IsActive = active,
        };
        context.Couples.Add(couple);

        return couple;
    }

    [Fact]
    public async Task SetCoupleActive_ShouldDeactivate_WhenThereAreNoBlockers_AndKeepPatientsActive()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var couple = AddCouple(context);
        await context.SaveChangesAsync();

        var result = await handler.SetCoupleActive(couple.Id, false, CancellationToken.None);

        Assert.True(result.Success);
        var saved = await context.Couples.FindAsync(couple.Id);
        Assert.NotNull(saved);
        Assert.False(saved.IsActive);

        var patient1 = await context.Patients.FindAsync(couple.Patient1Id);
        var patient2 = await context.Patients.FindAsync(couple.Patient2Id);
        Assert.True(patient1!.IsActive);
        Assert.True(patient2!.IsActive);
    }

    [Fact]
    public async Task SetCoupleActive_ShouldBlockDeactivation_WhenThereIsFutureCoupleAppointment()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var couple = AddCouple(context);
        context.Appointments.Add(
            new Appointment
            {
                Id = Guid.NewGuid(),
                CoupleId = couple.Id,
                AppointmentDate = DateTimeOffset.UtcNow.AddDays(2),
                AppointmentStatus = AppointmentStatusEnum.Marcada,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.SetCoupleActive(couple.Id, false, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("consultas futuras", result.Message);
        var saved = await context.Couples.FindAsync(couple.Id);
        Assert.True(saved!.IsActive);
    }

    [Fact]
    public async Task SetCoupleActive_ShouldBlockDeactivation_WhenThereIsActivePackageWithBalance()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var couple = AddCouple(context);
        context.AppointmentPackages.Add(
            new AppointmentPackage
            {
                Id = Guid.NewGuid(),
                CoupleId = couple.Id,
                TotalAppointments = 10,
                RemainingAppointments = 3,
                Status = PackageStatus.Ativo,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.SetCoupleActive(couple.Id, false, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("pacote", result.Message);
    }

    [Theory]
    [InlineData(ContractStatus.Ativo)]
    [InlineData(ContractStatus.AguardandoAceitacao)]
    public async Task SetCoupleActive_ShouldBlockDeactivation_WhenThereIsOpenContract(
        ContractStatus status
    )
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var couple = AddCouple(context);
        context.Contracts.Add(
            new Contract
            {
                Id = Guid.NewGuid(),
                CoupleId = couple.Id,
                Status = status,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.SetCoupleActive(couple.Id, false, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("contrato", result.Message);
    }

    [Fact]
    public async Task SetCoupleActive_ShouldRequireActivePatients_WhenReactivating()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var couple = AddCouple(context, active: false, patientsActive: false);
        await context.SaveChangesAsync();

        var blocked = await handler.SetCoupleActive(couple.Id, true, CancellationToken.None);
        Assert.False(blocked.Success);
        Assert.Contains("Reative os parceiros", blocked.Message);

        var patient1 = await context.Patients.FindAsync(couple.Patient1Id);
        var patient2 = await context.Patients.FindAsync(couple.Patient2Id);
        patient1!.IsActive = true;
        patient2!.IsActive = true;
        await context.SaveChangesAsync();

        var result = await handler.SetCoupleActive(couple.Id, true, CancellationToken.None);
        Assert.True(result.Success);

        var saved = await context.Couples.FindAsync(couple.Id);
        Assert.True(saved!.IsActive);
    }

    [Fact]
    public async Task GetCouples_ShouldFilterByActivationStatus()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        AddCouple(context, "Casal Ativo");
        AddCouple(context, "Casal Inativo", active: false);
        await context.SaveChangesAsync();

        var active = await handler.GetCouples(
            0,
            10,
            CancellationToken.None,
            CoupleStatusFilter.Active
        );
        var inactive = await handler.GetCouples(
            0,
            10,
            CancellationToken.None,
            CoupleStatusFilter.Inactive
        );

        Assert.Equal("Casal Ativo", Assert.Single(active.Items).Name);
        Assert.Equal("Casal Inativo", Assert.Single(inactive.Items).Name);
    }

    [Fact]
    public async Task QueryCouples_ShouldFilterByActivationStatus()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        AddCouple(context, "Casal Ativo");
        AddCouple(context, "Casal Inativo", active: false);
        await context.SaveChangesAsync();

        var result = await handler.QueryCouples(
            "Casal",
            0,
            10,
            CancellationToken.None,
            filter: CoupleStatusFilter.Inactive
        );

        Assert.Equal("Casal Inativo", Assert.Single(result.Items).Name);
    }

    [Fact]
    public async Task DeleteCouple_ShouldBlock_WhenThereIsAppointmentHistory()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var couple = AddCouple(context);
        context.Appointments.Add(
            new Appointment
            {
                Id = Guid.NewGuid(),
                CoupleId = couple.Id,
                AppointmentDate = DateTimeOffset.UtcNow.AddDays(-5),
                AppointmentStatus = AppointmentStatusEnum.Realizada,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.DeleteCouple(couple.Id);

        Assert.False(result.Success);
        Assert.Contains("Desative o casal", result.Message);
        Assert.NotNull(await context.Couples.FindAsync(couple.Id));
    }

    [Fact]
    public async Task DeleteCouple_ShouldBlock_WhenThereIsStandaloneNote()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var couple = AddCouple(context);
        context.AdditionalNotes.Add(
            new AdditionalNote
            {
                Id = Guid.NewGuid(),
                CoupleId = couple.Id,
                Content = "Nota do casal",
                NoteDate = DateTimeOffset.UtcNow,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.DeleteCouple(couple.Id);

        Assert.False(result.Success);
        Assert.NotNull(await context.Couples.FindAsync(couple.Id));
        Assert.NotNull(await context.AdditionalNotes.FirstOrDefaultAsync());
    }

    [Fact]
    public async Task UpdateCouple_ShouldPreserveActivationStatus()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var couple = AddCouple(context, active: false);
        await context.SaveChangesAsync();

        var update = new Couple
        {
            Id = couple.Id,
            Name = "Casal Renomeado",
            Patient1Id = couple.Patient1Id,
            Patient2Id = couple.Patient2Id,
            IsActive = true,
        };

        var result = await handler.UpdateCouple(update);

        Assert.True(result.Success);
        var saved = await context.Couples.FindAsync(couple.Id);
        Assert.NotNull(saved);
        Assert.Equal("Casal Renomeado", saved.Name);
        Assert.False(saved.IsActive);
    }
}
