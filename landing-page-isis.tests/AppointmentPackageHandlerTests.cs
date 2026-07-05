using landing_page_isis.core;
using landing_page_isis.core.Models;
using landing_page_isis.Handlers;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.tests;

public class AppointmentPackageHandlerTests
{
    private AppDbContext GetDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task CreatePackage_ShouldReturnTrue_WhenValid()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentPackageHandler(context);

        var pkg = new AppointmentPackage
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            TotalAppointments = 10,
            Price = 500,
            PaymentMethod = PaymentMethod.Pix,
        };

        var result = await handler.CreatePackage(pkg);

        Assert.True(result.Success);
        var saved = await context.AppointmentPackages.FindAsync(pkg.Id);
        Assert.NotNull(saved);
        Assert.Equal(10, saved.RemainingAppointments);
    }

    [Fact]
    public async Task CreatePackage_ShouldReturnFalse_WhenPatientIdEmpty()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentPackageHandler(context);

        var pkg = new AppointmentPackage
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.Empty,
            TotalAppointments = 10,
            Price = 500,
        };

        var result = await handler.CreatePackage(pkg);

        Assert.False(result.Success);
        Assert.Equal("Paciente ou casal não informado.", result.Message);
    }

    [Fact]
    public async Task CreatePackage_ShouldReturnFalse_WhenTotalAppointmentsZero()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentPackageHandler(context);

        var pkg = new AppointmentPackage
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            TotalAppointments = 0,
            Price = 500,
        };

        var result = await handler.CreatePackage(pkg);

        Assert.False(result.Success);
        Assert.Equal("O número total de consultas deve ser maior que zero.", result.Message);
    }

    [Fact]
    public async Task CreatePackage_ShouldReturnFalse_WhenPriceZero()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentPackageHandler(context);

        var pkg = new AppointmentPackage
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            TotalAppointments = 10,
            Price = 0,
        };

        var result = await handler.CreatePackage(pkg);

        Assert.False(result.Success);
        Assert.Equal("O preço deve ser maior que zero.", result.Message);
    }

    [Fact]
    public async Task UpdatePackage_ShouldReturnTrue_WhenValid()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentPackageHandler(context);

        var id = Guid.NewGuid();
        context.AppointmentPackages.Add(
            new AppointmentPackage
            {
                Id = id,
                PatientId = Guid.NewGuid(),
                TotalAppointments = 10,
                RemainingAppointments = 8,
                Price = 500,
                Status = PackageStatus.Ativo,
                CreatedAt = DateTimeOffset.UtcNow,
            }
        );
        await context.SaveChangesAsync();

        var updated = new AppointmentPackage
        {
            Id = id,
            PatientId = Guid.NewGuid(),
            TotalAppointments = 5,
            RemainingAppointments = 3,
            Price = 300,
            Status = PackageStatus.Esgotado,
            PaymentMethod = PaymentMethod.Credito,
        };

        var result = await handler.UpdatePackage(updated);

        Assert.True(result.Success);
        var db = await context.AppointmentPackages.FindAsync(id);
        Assert.NotNull(db);
        Assert.Equal(5, db.TotalAppointments);
        Assert.Equal(3, db.RemainingAppointments);
        Assert.Equal(300, db.Price);
        Assert.Equal(PackageStatus.Esgotado, db.Status);
    }

    [Fact]
    public async Task UpdatePackage_ShouldReturnFalse_WhenNotFound()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentPackageHandler(context);

        var result = await handler.UpdatePackage(new AppointmentPackage { Id = Guid.NewGuid() });

        Assert.False(result.Success);
        Assert.Equal("Pacote não encontrado.", result.Message);
    }

    [Fact]
    public async Task DeletePackage_ShouldReturnTrue_WhenExists()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentPackageHandler(context);

        var id = Guid.NewGuid();
        context.AppointmentPackages.Add(
            new AppointmentPackage
            {
                Id = id,
                PatientId = Guid.NewGuid(),
                TotalAppointments = 5,
                RemainingAppointments = 5,
                Price = 300,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.DeletePackage(id);

        Assert.True(result.Success);
        Assert.Null(await context.AppointmentPackages.FindAsync(id));
    }

    [Fact]
    public async Task DeletePackage_ShouldReturnFalse_WhenNotFound()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentPackageHandler(context);

        var result = await handler.DeletePackage(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("Pacote não encontrado.", result.Message);
    }

    [Fact]
    public async Task GetPackagesByPatientId_ShouldReturnPaginatedPackages()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentPackageHandler(context);

        var patientId = Guid.NewGuid();
        for (int i = 0; i < 8; i++)
        {
            context.AppointmentPackages.Add(
                new AppointmentPackage
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    TotalAppointments = 5,
                    RemainingAppointments = 5,
                    Price = 300,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-i),
                }
            );
        }
        await context.SaveChangesAsync();

        var result = await handler.GetPackagesByPatientId(0, 3, patientId, CancellationToken.None);

        Assert.Equal(8, result.TotalItems);
        Assert.Equal(3, result.Items.Count());
    }

    [Fact]
    public async Task GetPackagesByPatientId_ShouldReturnEmpty_WhenNone()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentPackageHandler(context);

        var result = await handler.GetPackagesByPatientId(
            0,
            10,
            Guid.NewGuid(),
            CancellationToken.None
        );

        Assert.Equal(0, result.TotalItems);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllPackagesByDateRange_ShouldReturnPackagesInRange()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentPackageHandler(context);

        context.AppointmentPackages.AddRange(
            new AppointmentPackage
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                TotalAppointments = 5,
                RemainingAppointments = 5,
                Price = 300,
                CreatedAt = new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero),
            },
            new AppointmentPackage
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                TotalAppointments = 5,
                RemainingAppointments = 5,
                Price = 400,
                CreatedAt = new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero),
            },
            new AppointmentPackage
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                TotalAppointments = 5,
                RemainingAppointments = 5,
                Price = 500,
                CreatedAt = new DateTimeOffset(2025, 12, 15, 0, 0, 0, TimeSpan.Zero),
            }
        );
        await context.SaveChangesAsync();

        var start = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2025, 6, 30, 23, 59, 59, TimeSpan.Zero);

        var result = await handler.GetAllPackagesByDateRange(start, end, CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllPackagesByDateRange_ShouldReturnEmpty_WhenNone()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentPackageHandler(context);

        var result = await handler.GetAllPackagesByDateRange(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1),
            CancellationToken.None
        );

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPackagesByCoupleId_ShouldReturnPaginatedPackages()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentPackageHandler(context);

        var coupleId = Guid.NewGuid();
        for (int i = 0; i < 4; i++)
        {
            context.AppointmentPackages.Add(new AppointmentPackage
            {
                Id = Guid.NewGuid(),
                CoupleId = coupleId,
                TotalAppointments = 10,
                RemainingAppointments = 10,
                Price = 800,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-i)
            });
        }
        await context.SaveChangesAsync();

        var result = await handler.GetPackagesByCoupleId(0, 2, coupleId, CancellationToken.None);

        Assert.Equal(4, result.TotalItems);
        Assert.Equal(2, result.Items.Count());
        Assert.All(result.Items, pkg => Assert.Equal(coupleId, pkg.CoupleId));
    }
}

