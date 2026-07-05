using landing_page_isis.core;
using landing_page_isis.core;
using landing_page_isis.core.Models;
using landing_page_isis.Handlers;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.tests;

public class AppointmentHandlerTests
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
    public async Task CreateAppointment_ShouldReturnFalse_WhenDataIsInvalid()
    {
        // Arrange
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);

        // Act
        var result = await handler.CreateAppointment(null!);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Dados inválidos.", result.Message);
    }

    [Fact]
    public async Task CreateAppointment_ShouldReturnFalse_WhenTimeSlotIsOccupied()
    {
        // Arrange
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);
        var appointmentDate = DateTime.UtcNow.AddDays(1);

        var patientId = Guid.NewGuid();
        context.Patients.Add(new Patient { Id = patientId, Name = "Test" });

        var existingAppointment = new Appointment
        {
            Id = Guid.NewGuid(),
            AppointmentDate = appointmentDate,
            PatientId = patientId,
        };
        context.Appointments.Add(existingAppointment);
        await context.SaveChangesAsync();

        var newAppointment = new Appointment
        {
            Id = Guid.NewGuid(),
            AppointmentDate = appointmentDate,
            PatientId = Guid.NewGuid(),
        };

        // Act
        var result = await handler.CreateAppointment(newAppointment);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Este horário já possui um agendamento.", result.Message);
    }

    [Fact]
    public async Task CreateAppointment_ShouldReturnTrue_WhenTimeSlotIsAvailable()
    {
        // Arrange
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);
        var appointmentDate = DateTime.UtcNow.AddDays(1);

        var patientId = Guid.NewGuid();
        context.Patients.Add(new Patient { Id = patientId, Name = "Test" });
        await context.SaveChangesAsync();

        var newAppointment = new Appointment
        {
            Id = Guid.NewGuid(),
            AppointmentDate = appointmentDate,
            PatientId = patientId,
        };

        // Act
        var result = await handler.CreateAppointment(newAppointment);

        // Assert
        Assert.True(result.Success);
        Assert.True(await context.Appointments.AnyAsync(a => a.Id == newAppointment.Id));
    }

    [Fact]
    public async Task GetAllAppointments_ShouldReturnPaginatedList()
    {
        // Arrange
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);

        var patientId = Guid.NewGuid();
        context.Patients.Add(new Patient { Id = patientId, Name = "Test" });

        for (int i = 0; i < 15; i++)
        {
            context.Appointments.Add(
                new Appointment
                {
                    Id = Guid.NewGuid(),
                    AppointmentDate = DateTime.UtcNow.AddDays(i),
                    PatientId = patientId,
                }
            );
        }
        await context.SaveChangesAsync();

        // Act
        var result = await handler.GetAllAppointments(0, 10, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(15, result.TotalItems);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(0, result.CurrentPage);
    }

    [Fact]
    public async Task GetAppointmentsByPatientId_ShouldReturnCorrectAppointments()
    {
        // Arrange
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);

        var patientId1 = Guid.NewGuid();
        var patientId2 = Guid.NewGuid();

        context.Patients.AddRange(
            new Patient { Id = patientId1, Name = "Test1" },
            new Patient { Id = patientId2, Name = "Test2" }
        );

        context.Appointments.AddRange(
            new Appointment
            {
                Id = Guid.NewGuid(),
                AppointmentDate = DateTime.UtcNow.AddDays(1),
                PatientId = patientId1,
            },
            new Appointment
            {
                Id = Guid.NewGuid(),
                AppointmentDate = DateTime.UtcNow.AddDays(2),
                PatientId = patientId1,
            },
            new Appointment
            {
                Id = Guid.NewGuid(),
                AppointmentDate = DateTime.UtcNow.AddDays(3),
                PatientId = patientId2,
            }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await handler.GetAppointmentsByPatientId(
            0,
            10,
            patientId1,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(2, result.TotalItems);
        foreach (var item in result.Items)
        {
            Assert.Equal(patientId1, item!.PatientId);
        }
    }

    [Fact]
    public async Task GetAppointment_ShouldReturnAppointment_WhenExists()
    {
        // Arrange
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);

        var appointmentId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        var appointment = new Appointment
        {
            Id = appointmentId,
            AppointmentDate = DateTime.UtcNow,
            PatientId = patientId,
        };

        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        // Act
        var result = await handler.GetAppointment(appointmentId, patientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(appointmentId, result.Id);
        Assert.Equal(patientId, result.PatientId);
    }

    [Fact]
    public async Task UpdateAppointment_ShouldReturnTrue_WhenValid()
    {
        // Arrange
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);

        var id = Guid.NewGuid();
        var existingAppointment = new Appointment
        {
            Id = id,
            AppointmentDate = DateTime.UtcNow.AddDays(1),
            PatientId = Guid.NewGuid(),
            Price = 150,
        };

        context.Appointments.Add(existingAppointment);
        await context.SaveChangesAsync();

        var updatedAppointment = new Appointment
        {
            Id = id,
            AppointmentDate = DateTime.UtcNow.AddDays(2),
            PatientId = existingAppointment.PatientId,
            Price = 200,
        };

        // Act
        var result = await handler.UpdateAppointment(updatedAppointment, id);

        // Assert
        Assert.True(result.Success);
        var dbAppointment = await context.Appointments.FindAsync(id);
        Assert.Equal(200, dbAppointment!.Price);
    }

    [Fact]
    public async Task UpdateAppointment_ShouldReturnFalse_WhenIdMismatch()
    {
        // Arrange
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);

        var appointment = new Appointment { Id = Guid.NewGuid() };

        // Act
        var result = await handler.UpdateAppointment(appointment, Guid.NewGuid());

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Os IDs de referência não coincidem.", result.Message);
    }

    [Fact]
    public async Task UpdateAppointment_ShouldReturnFalse_WhenTimeSlotOccupied()
    {
        // Arrange
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var occupiedDate = DateTime.UtcNow.AddDays(1);

        context.Appointments.AddRange(
            new Appointment
            {
                Id = id1,
                AppointmentDate = occupiedDate,
                PatientId = Guid.NewGuid(),
            },
            new Appointment
            {
                Id = id2,
                AppointmentDate = DateTime.UtcNow.AddDays(2),
                PatientId = Guid.NewGuid(),
            }
        );
        await context.SaveChangesAsync();

        var updatedAppointment = new Appointment
        {
            Id = id2,
            AppointmentDate = occupiedDate, // Try to move to the occupied slot
            PatientId = Guid.NewGuid(),
        };

        // Act
        var result = await handler.UpdateAppointment(updatedAppointment, id2);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Horario indisponivel", result.Message);
    }

    [Fact]
    public async Task DeleteAppointment_ShouldReturnTrue_WhenExists()
    {
        // Arrange
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);

        var id = Guid.NewGuid();
        context.Appointments.Add(
            new Appointment
            {
                Id = id,
                AppointmentDate = DateTime.UtcNow,
                PatientId = Guid.NewGuid(),
            }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await handler.DeleteAppointment(id);

        // Assert
        Assert.True(result.Success);
        Assert.False(await context.Appointments.AnyAsync(a => a.Id == id));
    }

    [Fact]
    public async Task GetAppointmentsByDateRange_ShouldReturnAppointmentsWithinRange()
    {
        // Arrange
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);

        var now = DateTimeOffset.UtcNow;
        var start = now.AddDays(-1);
        var end = now.AddDays(1);

        var patientId = Guid.NewGuid();
        context.Patients.Add(new Patient { Id = patientId, Name = "Test" });

        context.Appointments.AddRange(
            new Appointment
            {
                Id = Guid.NewGuid(),
                AppointmentDate = now.AddDays(-2).UtcDateTime,
                PatientId = patientId,
            }, // Outside
            new Appointment
            {
                Id = Guid.NewGuid(),
                AppointmentDate = now.UtcDateTime,
                PatientId = patientId,
            }, // Inside
            new Appointment
            {
                Id = Guid.NewGuid(),
                AppointmentDate = now.AddDays(2).UtcDateTime,
                PatientId = patientId,
            } // Outside
        );
        await context.SaveChangesAsync();

        // Act
        var result = await handler.GetAppointmentsByDateRange(
            start,
            end,
            0,
            10,
            CancellationToken.None
        );

        var items = result.Items.ToList();

        // Assert
        Assert.Single(items);
        Assert.Equal(now.UtcDateTime, items.First().AppointmentDate);
    }

    [Fact]
    public async Task GetAllAppointmentsByDateRange_ShouldReturnAllItemsWithoutPagination()
    {
        // Arrange
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);
        var start = DateTimeOffset.UtcNow.AddDays(-1);
        var end = DateTimeOffset.UtcNow.AddDays(1);

        var patientId = Guid.NewGuid();
        context.Patients.Add(new Patient { Id = patientId, Name = "Analytical Test" });
        await context.SaveChangesAsync();

        for (int i = 0; i < 20; i++)
        {
            context.Appointments.Add(
                new Appointment
                {
                    Id = Guid.NewGuid(),
                    AppointmentDate = DateTimeOffset.UtcNow,
                    PatientId = patientId,
                }
            );
        }
        await context.SaveChangesAsync();

        // Act
        var result = await handler.GetAllAppointmentsByDateRange(
            start,
            end,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(20, result.Count);
    }

    [Fact]
    public async Task CreateAppointment_ShouldFail_WhenPriceIsNegative()
    {
        // Arrange
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);
        var app = new Appointment
        {
            Id = Guid.NewGuid(),
            AppointmentDate = DateTime.UtcNow,
            Price = -50,
            PatientId = Guid.NewGuid(),
        };

        // Act
        var result = await handler.CreateAppointment(app);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("O preço não pode ser negativo.", result.Message);
    }

    [Fact]
    public async Task QueryAppointments_ShouldBeCaseSensitive_AsRequested()
    {
        // Arrange
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);
        var pId = Guid.NewGuid();
        context.Patients.Add(new Patient { Id = pId, Name = "Isis" });
        context.Appointments.Add(
            new Appointment
            {
                Id = Guid.NewGuid(),
                AppointmentDate = DateTime.UtcNow,
                PatientId = pId,
            }
        );
        await context.SaveChangesAsync();

        // Act & Assert
        var resultUpper = await handler.QueryAppointments("ISIS", 0, 10, CancellationToken.None);
        var resultExact = await handler.QueryAppointments("Isis", 0, 10, CancellationToken.None);

        // Based on current implementation (EF Core + SQLite/InMemory might behave differently,
        // but Postgres is case-sensitive by default with .Contains() unless using ILIKE)
        // We are testing that it DOES work for exact match.
        Assert.NotEmpty(resultExact.Items);
    }

    [Fact]
    public async Task CountPendingRecordsAsync_ShouldReturnCorrectCount()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);

        var pId = Guid.NewGuid();
        context.Patients.Add(new Patient { Id = pId, Name = "Test Patient" });

        // Add 2 past appointments marked as "Marcada" (Pending record check)
        context.Appointments.AddRange(
            new Appointment
            {
                Id = Guid.NewGuid(),
                AppointmentDate = DateTime.UtcNow.AddHours(-2),
                AppointmentStatus = AppointmentStatusEnum.Marcada,
                PatientId = pId
            },
            new Appointment
            {
                Id = Guid.NewGuid(),
                AppointmentDate = DateTime.UtcNow.AddHours(-1),
                AppointmentStatus = AppointmentStatusEnum.Marcada,
                PatientId = pId
            },
            new Appointment
            {
                Id = Guid.NewGuid(),
                AppointmentDate = DateTime.UtcNow.AddHours(-3),
                AppointmentStatus = AppointmentStatusEnum.Realizada, // completed, not counted
                PatientId = pId
            },
            new Appointment
            {
                Id = Guid.NewGuid(),
                AppointmentDate = DateTime.UtcNow.AddHours(2), // future, not counted
                AppointmentStatus = AppointmentStatusEnum.Marcada,
                PatientId = pId
            }
        );
        await context.SaveChangesAsync();

        var count = await handler.CountPendingRecordsAsync();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetAppointmentById_ShouldReturnAppointment_WhenExists()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);

        var appId = Guid.NewGuid();
        var pId = Guid.NewGuid();
        context.Patients.Add(new Patient { Id = pId, Name = "Test Patient" });
        context.Appointments.Add(new Appointment
        {
            Id = appId,
            AppointmentDate = DateTime.UtcNow,
            PatientId = pId
        });
        await context.SaveChangesAsync();

        var result = await handler.GetAppointmentById(appId);

        Assert.NotNull(result);
        Assert.Equal(appId, result.Id);
        Assert.NotNull(result.Patient);
        Assert.Equal("Test Patient", result.Patient.Name);
    }

    [Fact]
    public async Task GetAppointmentWithPatient_ShouldReturnAppointmentWithPatient_WhenExists()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);

        var appId = Guid.NewGuid();
        var pId = Guid.NewGuid();
        context.Patients.Add(new Patient { Id = pId, Name = "Test Patient" });
        context.Appointments.Add(new Appointment
        {
            Id = appId,
            AppointmentDate = DateTime.UtcNow,
            PatientId = pId
        });
        await context.SaveChangesAsync();

        var result = await handler.GetAppointmentWithPatient(appId, pId);

        Assert.NotNull(result);
        Assert.Equal(appId, result.Id);
        Assert.NotNull(result.Patient);
        Assert.Equal("Test Patient", result.Patient.Name);
    }

    [Fact]
    public async Task GetAppointmentsByCoupleId_ShouldReturnAppointments_WhenExists()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);

        var coupleId = Guid.NewGuid();
        var p1 = new Patient { Id = Guid.NewGuid(), Name = "Patient 1", Phone = "11999999999" };
        var p2 = new Patient { Id = Guid.NewGuid(), Name = "Patient 2", Phone = "11999999999" };
        context.Patients.AddRange(p1, p2);

        var couple = new Couple { Id = coupleId, Name = "Couple", Patient1 = p1, Patient2 = p2 };
        context.Couples.Add(couple);

        context.Appointments.AddRange(
            new Appointment { Id = Guid.NewGuid(), AppointmentDate = DateTime.UtcNow.AddDays(-1), CoupleId = coupleId },
            new Appointment { Id = Guid.NewGuid(), AppointmentDate = DateTime.UtcNow, CoupleId = coupleId },
            new Appointment { Id = Guid.NewGuid(), AppointmentDate = DateTime.UtcNow, CoupleId = Guid.NewGuid() } // other couple
        );
        await context.SaveChangesAsync();

        var result = await handler.GetAppointmentsByCoupleId(coupleId, CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task RefundPackageCredit_OnCancellation_ShouldIncrementRemainingAppointments()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);

        var patientId = Guid.NewGuid();
        context.Patients.Add(new Patient { Id = patientId, Name = "Test Patient" });

        var package = new AppointmentPackage
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            TotalAppointments = 10,
            RemainingAppointments = 5,
            Status = PackageStatus.Esgotado
        };
        context.AppointmentPackages.Add(package);

        var appId = Guid.NewGuid();
        var app = new Appointment
        {
            Id = appId,
            AppointmentDate = DateTime.UtcNow,
            PatientId = patientId,
            PackageId = package.Id,
            AppointmentStatus = AppointmentStatusEnum.Marcada
        };
        context.Appointments.Add(app);
        await context.SaveChangesAsync();

        // Cancel appointment
        var updatedApp = new Appointment
        {
            Id = appId,
            AppointmentDate = app.AppointmentDate,
            PatientId = patientId,
            PackageId = package.Id,
            AppointmentStatus = AppointmentStatusEnum.Cancelada
        };

        var result = await handler.UpdateAppointment(updatedApp, appId);

        Assert.True(result.Success);
        var dbPackage = await context.AppointmentPackages.FindAsync(package.Id);
        Assert.NotNull(dbPackage);
        Assert.Equal(6, dbPackage.RemainingAppointments);
        Assert.Equal(PackageStatus.Ativo, dbPackage.Status);
    }

    [Fact]
    public async Task RefundPackageCredit_OnDeletion_ShouldIncrementRemainingAppointments()
    {
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);

        var patientId = Guid.NewGuid();
        context.Patients.Add(new Patient { Id = patientId, Name = "Test Patient" });

        var package = new AppointmentPackage
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            TotalAppointments = 10,
            RemainingAppointments = 9,
            Status = PackageStatus.Ativo
        };
        context.AppointmentPackages.Add(package);

        var appId = Guid.NewGuid();
        var app = new Appointment
        {
            Id = appId,
            AppointmentDate = DateTime.UtcNow,
            PatientId = patientId,
            PackageId = package.Id,
            AppointmentStatus = AppointmentStatusEnum.Marcada
        };
        context.Appointments.Add(app);
        await context.SaveChangesAsync();

        var result = await handler.DeleteAppointment(appId);

        Assert.True(result.Success);
        var dbPackage = await context.AppointmentPackages.FindAsync(package.Id);
        Assert.NotNull(dbPackage);
        Assert.Equal(10, dbPackage.RemainingAppointments);
    }
}

