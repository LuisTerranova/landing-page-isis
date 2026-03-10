using landing_page_isis.core.Models;
using landing_page_isis.Data;
using landing_page_isis.Handlers;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.tests;

public class PacientHandlerTests
{
    private AppDbContext GetDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var databaseContext = new AppDbContext(options);
        databaseContext.Database.EnsureCreated();

        Environment.SetEnvironmentVariable("ENCRYPTION_KEY", "test-encryption-key-32-bytes----");

        return databaseContext;
    }

    [Fact]
    public async Task CreatePacient_ShouldReturnFalse_WhenNull()
    {
        await using var context = GetDatabaseContext();
        var handler = new PacientHandler(context);

        var result = await handler.CreatePacient(null);

        Assert.False(result.Success);
        Assert.Equal("Dados inválidos.", result.Message);
    }

    [Fact]
    public async Task CreatePacient_ShouldReturnTrue_AndFormatPhoneAndCpf()
    {
        await using var context = GetDatabaseContext();
        var handler = new PacientHandler(context);

        var pacient = new Pacient
        {
            Id = Guid.NewGuid(),
            Name = "Test Pacient",
            Phone = "(11) 98765-4321",
            Cpf = "123.456.789-00",
            Email = "test@example.com",
        };

        var result = await handler.CreatePacient(pacient);

        Assert.True(result.Success);

        var dbPacient = await context.Pacients.FindAsync(pacient.Id);
        Assert.NotNull(dbPacient);
        Assert.Equal("11987654321", dbPacient.Phone);
        Assert.Equal("12345678900", dbPacient.Cpf);
    }

    [Fact]
    public async Task GetPacients_ShouldReturnPaginatedList()
    {
        await using var context = GetDatabaseContext();
        var handler = new PacientHandler(context);

        for (int i = 0; i < 5; i++)
        {
            context.Pacients.Add(
                new Pacient
                {
                    Id = Guid.NewGuid(),
                    Name = $"Pacient {i}",
                    Phone = "",
                    Cpf = "",
                    Email = "",
                }
            );
        }
        await context.SaveChangesAsync();

        var result = await handler.GetPacients(0, 3, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(5, result.TotalItems);
        Assert.Equal(3, result.Items.Count());
    }

    [Fact]
    public async Task GetPacient_ShouldReturnPacient_WhenExists()
    {
        await using var context = GetDatabaseContext();
        var handler = new PacientHandler(context);

        var id = Guid.NewGuid();
        context.Pacients.Add(
            new Pacient
            {
                Id = id,
                Name = "Existing",
                Phone = "",
                Cpf = "",
                Email = "",
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.GetPacient(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Existing", result.Name);
    }

    [Fact]
    public async Task UpdatePacient_ShouldReturnTrue_WhenValid_AndFormatPhoneAndCpf()
    {
        await using var context = GetDatabaseContext();
        var handler = new PacientHandler(context);

        var id = Guid.NewGuid();
        context.Pacients.Add(
            new Pacient
            {
                Id = id,
                Name = "Old Name",
                Phone = "000",
                Cpf = "111",
                Email = "old@example.com",
            }
        );
        await context.SaveChangesAsync();

        var updated = new Pacient
        {
            Id = id,
            Name = "New Name",
            Phone = "(22) 1234-5678",
            Cpf = "987.654.321-11",
            Email = "new@example.com",
        };

        var result = await handler.UpdatePacient(updated);

        Assert.True(result.Success);

        var dbPacient = await context.Pacients.FindAsync(id);
        Assert.NotNull(dbPacient);
        Assert.Equal("New Name", dbPacient.Name);
        Assert.Equal("2212345678", dbPacient.Phone);
        Assert.Equal("98765432111", dbPacient.Cpf);
    }

    [Fact]
    public async Task UpdatePacient_ShouldReturnFalse_WhenNotFound()
    {
        await using var context = GetDatabaseContext();
        var handler = new PacientHandler(context);

        var pacient = new Pacient
        {
            Id = Guid.NewGuid(),
            Name = "Ghost",
            Phone = "",
            Cpf = "",
            Email = "",
        };

        var result = await handler.UpdatePacient(pacient);

        Assert.False(result.Success);
        Assert.Equal("Paciente não encontrado.", result.Message);
    }

    [Fact]
    public async Task DeletePacient_ShouldReturnTrue_WhenExists()
    {
        await using var context = GetDatabaseContext();
        var handler = new PacientHandler(context);

        var id = Guid.NewGuid();
        context.Pacients.Add(
            new Pacient
            {
                Id = id,
                Name = "To Delete",
                Phone = "",
                Cpf = "",
                Email = "",
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.DeletePacient(id);

        Assert.True(result.Success);
        Assert.False(await context.Pacients.AnyAsync(p => p.Id == id));
    }

    [Fact]
    public async Task DeletePacient_ShouldReturnFalse_WhenNotFound()
    {
        await using var context = GetDatabaseContext();
        var handler = new PacientHandler(context);

        var result = await handler.DeletePacient(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("Paciente não encontrado.", result.Message);
    }
}
