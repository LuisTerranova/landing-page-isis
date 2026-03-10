using landing_page_isis.core.Models;
using landing_page_isis.Data;
using landing_page_isis.Handlers;
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

        var pacientId = Guid.NewGuid();
        context.Pacients.Add(new Pacient { Id = pacientId, Name = "Test" });

        var existingAppointment = new Appointment
        {
            Id = Guid.NewGuid(),
            AppointmentDate = appointmentDate,
            PacientId = pacientId,
        };
        context.Appointments.Add(existingAppointment);
        await context.SaveChangesAsync();

        var newAppointment = new Appointment
        {
            Id = Guid.NewGuid(),
            AppointmentDate = appointmentDate,
            PacientId = Guid.NewGuid(),
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

        var pacientId = Guid.NewGuid();
        context.Pacients.Add(new Pacient { Id = pacientId, Name = "Test" });
        await context.SaveChangesAsync();

        var newAppointment = new Appointment
        {
            Id = Guid.NewGuid(),
            AppointmentDate = appointmentDate,
            PacientId = pacientId,
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

        var pacientId = Guid.NewGuid();
        context.Pacients.Add(new Pacient { Id = pacientId, Name = "Test" });

        for (int i = 0; i < 15; i++)
        {
            context.Appointments.Add(
                new Appointment
                {
                    Id = Guid.NewGuid(),
                    AppointmentDate = DateTime.UtcNow.AddDays(i),
                    PacientId = pacientId,
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
    public async Task GetAppointmentsByPacientId_ShouldReturnCorrectAppointments()
    {
        // Arrange
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);

        var pacientId1 = Guid.NewGuid();
        var pacientId2 = Guid.NewGuid();

        context.Pacients.AddRange(
            new Pacient { Id = pacientId1, Name = "Test1" },
            new Pacient { Id = pacientId2, Name = "Test2" }
        );

        context.Appointments.AddRange(
            new Appointment
            {
                Id = Guid.NewGuid(),
                AppointmentDate = DateTime.UtcNow.AddDays(1),
                PacientId = pacientId1,
            },
            new Appointment
            {
                Id = Guid.NewGuid(),
                AppointmentDate = DateTime.UtcNow.AddDays(2),
                PacientId = pacientId1,
            },
            new Appointment
            {
                Id = Guid.NewGuid(),
                AppointmentDate = DateTime.UtcNow.AddDays(3),
                PacientId = pacientId2,
            }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await handler.GetAppointmentsByPacientId(
            0,
            10,
            pacientId1,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(2, result.TotalItems);
        foreach (var item in result.Items)
        {
            Assert.Equal(pacientId1, item!.PacientId);
        }
    }

    [Fact]
    public async Task GetAppointment_ShouldReturnAppointment_WhenExists()
    {
        // Arrange
        await using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);

        var appointmentId = Guid.NewGuid();
        var pacientId = Guid.NewGuid();

        var appointment = new Appointment
        {
            Id = appointmentId,
            AppointmentDate = DateTime.UtcNow,
            PacientId = pacientId,
        };

        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        // Act
        var result = await handler.GetAppointment(appointmentId, pacientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(appointmentId, result.Id);
        Assert.Equal(pacientId, result.PacientId);
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
            PacientId = Guid.NewGuid(),
            Price = 150,
        };

        context.Appointments.Add(existingAppointment);
        await context.SaveChangesAsync();

        var updatedAppointment = new Appointment
        {
            Id = id,
            AppointmentDate = DateTime.UtcNow.AddDays(2),
            PacientId = existingAppointment.PacientId,
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
                PacientId = Guid.NewGuid(),
            },
            new Appointment
            {
                Id = id2,
                AppointmentDate = DateTime.UtcNow.AddDays(2),
                PacientId = Guid.NewGuid(),
            }
        );
        await context.SaveChangesAsync();

        var updatedAppointment = new Appointment
        {
            Id = id2,
            AppointmentDate = occupiedDate, // Try to move to the occupied slot
            PacientId = Guid.NewGuid(),
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
                PacientId = Guid.NewGuid(),
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

        var pacientId = Guid.NewGuid();
        context.Pacients.Add(new Pacient { Id = pacientId, Name = "Test" });

        context.Appointments.AddRange(
            new Appointment
            {
                Id = Guid.NewGuid(),
                AppointmentDate = now.AddDays(-2).UtcDateTime,
                PacientId = pacientId,
            }, // Outside
            new Appointment
            {
                Id = Guid.NewGuid(),
                AppointmentDate = now.UtcDateTime,
                PacientId = pacientId,
            }, // Inside
            new Appointment
            {
                Id = Guid.NewGuid(),
                AppointmentDate = now.AddDays(2).UtcDateTime,
                PacientId = pacientId,
            } // Outside
        );
        await context.SaveChangesAsync();

        // Act
        var result = await handler.GetAppointmentsByDateRange(start, end, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(now.UtcDateTime, result.First().AppointmentDate);
    }
}
