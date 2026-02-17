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
        return databaseContext;
    }

    [Fact]
    public async Task CreateAppointment_ShouldReturnFalse_WhenTimeSlotIsOccupied()
    {
        // Arrange
        using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);
        var appointmentDate = DateTime.Now.AddDays(1);

        var existingAppointment = new Appointment
        {
            Id = Guid.NewGuid(),
            AppointmentDate = appointmentDate,
            PacientId = Guid.NewGuid()
        };
        context.Appointments.Add(existingAppointment);
        await context.SaveChangesAsync();

        var newAppointment = new Appointment
        {
            Id = Guid.NewGuid(),
            AppointmentDate = appointmentDate,
            PacientId = Guid.NewGuid()
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
        using var context = GetDatabaseContext();
        var handler = new AppointmentHandler(context);
        var appointmentDate = DateTime.Now.AddDays(1);

        var newAppointment = new Appointment
        {
            Id = Guid.NewGuid(),
            AppointmentDate = appointmentDate,
            PacientId = Guid.NewGuid()
        };

        // Act
        var result = await handler.CreateAppointment(newAppointment);

        // Assert
        Assert.True(result.Success);
        Assert.True(await context.Appointments.AnyAsync(a => a.Id == newAppointment.Id));
    }
}
