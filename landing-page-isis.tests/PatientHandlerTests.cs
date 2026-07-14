using landing_page_isis.core.Models;
using landing_page_isis.Handlers;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.tests;

public class PatientHandlerTests
{
    private AppDbContext GetDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var databaseContext = new AppDbContext(options);
        databaseContext.Database.EnsureCreated();

        Environment.SetEnvironmentVariable("ENCRYPTION_KEY", "test-encryption-key-32-bytes----");
        Environment.SetEnvironmentVariable("CPF_HASH_PEPPER", "test-pepper-secret");

        return databaseContext;
    }

    [Fact]
    public async Task CreatePatient_ShouldReturnFalse_WhenNull()
    {
        await using var context = GetDatabaseContext();
        var handler = new PatientHandler(context);

        var result = await handler.CreatePatient(null);

        Assert.False(result.Success);
        Assert.Equal("Dados inválidos.", result.Message);
    }

    [Fact]
    public async Task CreatePatient_ShouldReturnTrue_AndFormatPhoneAndCpf()
    {
        await using var context = GetDatabaseContext();
        var handler = new PatientHandler(context);

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Test Patient",
            Phone = "(11) 98765-4321",
            Cpf = "529.982.247-25",
            Email = "test@example.com",
        };

        var result = await handler.CreatePatient(patient);

        Assert.True(result.Success);

        var dbPatient = await context.Patients.FindAsync(patient.Id);
        Assert.NotNull(dbPatient);
        Assert.Equal("11987654321", dbPatient.Phone);
        Assert.Equal("52998224725", dbPatient.Cpf);
    }

    [Fact]
    public async Task GetPatients_ShouldReturnPaginatedList()
    {
        await using var context = GetDatabaseContext();
        var handler = new PatientHandler(context);

        for (int i = 0; i < 5; i++)
        {
            context.Patients.Add(
                new Patient
                {
                    Id = Guid.NewGuid(),
                    Name = $"Patient {i}",
                    Phone = "",
                    Cpf = "",
                    Email = "",
                }
            );
        }
        await context.SaveChangesAsync();

        var result = await handler.GetPatients(0, 3, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(5, result.TotalItems);
        Assert.Equal(3, result.Items.Count());
    }

    [Fact]
    public async Task GetPatient_ShouldReturnPatient_WhenExists()
    {
        await using var context = GetDatabaseContext();
        var handler = new PatientHandler(context);

        var id = Guid.NewGuid();
        context.Patients.Add(
            new Patient
            {
                Id = id,
                Name = "Existing",
                Phone = "",
                Cpf = "",
                Email = "",
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.GetPatient(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Existing", result.Name);
    }

    [Fact]
    public async Task UpdatePatient_ShouldReturnTrue_WhenValid_AndFormatPhoneAndCpf()
    {
        await using var context = GetDatabaseContext();
        var handler = new PatientHandler(context);

        var id = Guid.NewGuid();
        context.Patients.Add(
            new Patient
            {
                Id = id,
                Name = "Old Name",
                Phone = "000",
                Cpf = "111",
                Email = "old@example.com",
            }
        );
        await context.SaveChangesAsync();

        var updated = new Patient
        {
            Id = id,
            Name = "New Name",
            Phone = "(22) 1234-5678",
            Cpf = "529.982.247-25",
            Email = "new@example.com",
        };

        var result = await handler.UpdatePatient(updated);

        Assert.True(result.Success);

        var dbPatient = await context.Patients.FindAsync(id);
        Assert.NotNull(dbPatient);
        Assert.Equal("New Name", dbPatient.Name);
        Assert.Equal("2212345678", dbPatient.Phone);
        Assert.Equal("52998224725", dbPatient.Cpf);
    }

    [Fact]
    public async Task UpdatePatient_ShouldReturnFalse_WhenNotFound()
    {
        await using var context = GetDatabaseContext();
        var handler = new PatientHandler(context);

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "Ghost",
            Phone = "",
            Cpf = "",
            Email = "",
        };

        var result = await handler.UpdatePatient(patient);

        Assert.False(result.Success);
        Assert.Equal("Paciente não encontrado.", result.Message);
    }

    [Fact]
    public async Task DeletePatient_ShouldReturnTrue_WhenExists()
    {
        await using var context = GetDatabaseContext();
        var handler = new PatientHandler(context);

        var id = Guid.NewGuid();
        context.Patients.Add(
            new Patient
            {
                Id = id,
                Name = "To Delete",
                Phone = "",
                Cpf = "",
                Email = "",
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.DeletePatient(id);

        Assert.True(result.Success);
        Assert.False(await context.Patients.AnyAsync(p => p.Id == id));
    }

    [Fact]
    public async Task DeletePatient_ShouldReturnFalse_WhenNotFound()
    {
        await using var context = GetDatabaseContext();
        var handler = new PatientHandler(context);

        var result = await handler.DeletePatient(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("Paciente não encontrado.", result.Message);
    }

    [Fact]
    public async Task GetPatientEmailMap_ShouldReturnCorrectMap()
    {
        await using var context = GetDatabaseContext();
        var handler = new PatientHandler(context);

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        var p1 = new Patient { Id = id1, Name = "A", Email = "a@test.com", Phone = "11" };
        var p2 = new Patient { Id = id2, Name = "B", Email = "b@test.com", Phone = "22" };
        var p3 = new Patient { Id = id3, Name = "C", Email = null, Phone = "33" };
        context.Patients.AddRange(p1, p2, p3);
        await context.SaveChangesAsync();

        var ids = new List<Guid> { id1, id2, id3, Guid.NewGuid() };
        var map = await handler.GetPatientEmailMap(ids, CancellationToken.None);

        Assert.Equal(3, map.Count);
        Assert.Equal("a@test.com", map[id1]);
        Assert.Equal("b@test.com", map[id2]);
        Assert.Null(map[id3]);
    }

    [Fact]
    public async Task QueryPatients_ShouldReturnFilteredPatients()
    {
        await using var context = GetDatabaseContext();
        var handler = new PatientHandler(context);

        context.Patients.AddRange(
            new Patient { Id = Guid.NewGuid(), Name = "John Doe", Phone = "11" },
            new Patient { Id = Guid.NewGuid(), Name = "Jane Doe", Phone = "22" },
            new Patient { Id = Guid.NewGuid(), Name = "Bob Smith", Phone = "33" }
        );
        await context.SaveChangesAsync();

        var result = await handler.QueryPatients("Doe", 0, 10, CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Contains(result.Items, p => p.Name == "John Doe");
        Assert.Contains(result.Items, p => p.Name == "Jane Doe");
    }
}

