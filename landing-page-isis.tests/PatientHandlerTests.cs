using landing_page_isis.core;
using landing_page_isis.core.Models;
using landing_page_isis.Extensions;
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

    private PatientHandler GetHandler(AppDbContext context)
    {
        return new PatientHandler(
            context,
            new landing_page_isis.core.Validators.PatientValidator()
        );
    }

    [Fact]
    public async Task CreatePatient_ShouldReturnFalse_WhenNull()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var result = await handler.CreatePatient(null);

        Assert.False(result.Success);
        Assert.Equal("Dados inválidos.", result.Message);
    }

    [Fact]
    public async Task CreatePatient_ShouldReturnTrue_AndFormatPhoneAndCpf()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

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
        var handler = GetHandler(context);

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
        var handler = GetHandler(context);

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
        var handler = GetHandler(context);

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
        var handler = GetHandler(context);

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
        var handler = GetHandler(context);

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
        var handler = GetHandler(context);

        var result = await handler.DeletePatient(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("Paciente não encontrado.", result.Message);
    }

    [Fact]
    public async Task GetPatientEmailMap_ShouldReturnCorrectMap()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        var p1 = new Patient
        {
            Id = id1,
            Name = "A",
            Email = "a@test.com",
            Phone = "11",
        };
        var p2 = new Patient
        {
            Id = id2,
            Name = "B",
            Email = "b@test.com",
            Phone = "22",
        };
        var p3 = new Patient
        {
            Id = id3,
            Name = "C",
            Email = null,
            Phone = "33",
        };
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
        var handler = GetHandler(context);

        context.Patients.AddRange(
            new Patient
            {
                Id = Guid.NewGuid(),
                Name = "John Doe",
                Phone = "11",
            },
            new Patient
            {
                Id = Guid.NewGuid(),
                Name = "Jane Doe",
                Phone = "22",
            },
            new Patient
            {
                Id = Guid.NewGuid(),
                Name = "Bob Smith",
                Phone = "33",
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.QueryPatients("Doe", 0, 10, CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Contains(result.Items, p => p.Name == "John Doe");
        Assert.Contains(result.Items, p => p.Name == "Jane Doe");
    }

    private static DateOnly TodayPortoVelho()
    {
        return DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToPortoVelhoTime());
    }

    private static DateOnly BirthDateForMonthDay(DateOnly date)
    {
        return new DateOnly(2000, date.Month, date.Day);
    }

    [Fact]
    public async Task GetPatients_ShouldReturnOnlyActiveByDefault()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        context.Patients.AddRange(
            new Patient
            {
                Id = Guid.NewGuid(),
                Name = "Ativo",
                Phone = "1",
                IsActive = true,
            },
            new Patient
            {
                Id = Guid.NewGuid(),
                Name = "Inativo",
                Phone = "2",
                IsActive = false,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.GetPatients(0, 10, CancellationToken.None);

        Assert.Equal(1, result.TotalItems);
        Assert.Equal("Ativo", result.Items.Single().Name);
    }

    [Fact]
    public async Task GetPatients_ShouldReturnOnlyInactive_WhenFilterIsInactive()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        context.Patients.AddRange(
            new Patient
            {
                Id = Guid.NewGuid(),
                Name = "Ativo",
                Phone = "1",
                IsActive = true,
            },
            new Patient
            {
                Id = Guid.NewGuid(),
                Name = "Inativo",
                Phone = "2",
                IsActive = false,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.GetPatients(
            0,
            10,
            CancellationToken.None,
            PatientStatusFilter.Inactive
        );

        Assert.Equal(1, result.TotalItems);
        Assert.Equal("Inativo", result.Items.Single().Name);
    }

    [Fact]
    public async Task GetPatients_ShouldReturnAll_WhenFilterIsAll()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        context.Patients.AddRange(
            new Patient
            {
                Id = Guid.NewGuid(),
                Name = "Ativo",
                Phone = "1",
                IsActive = true,
            },
            new Patient
            {
                Id = Guid.NewGuid(),
                Name = "Inativo",
                Phone = "2",
                IsActive = false,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.GetPatients(
            0,
            10,
            CancellationToken.None,
            PatientStatusFilter.All
        );

        Assert.Equal(2, result.TotalItems);
    }

    [Fact]
    public async Task QueryPatients_ShouldExcludeDeactivatedByDefault()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        context.Patients.AddRange(
            new Patient
            {
                Id = Guid.NewGuid(),
                Name = "Maria Ativa",
                Phone = "1",
                IsActive = true,
            },
            new Patient
            {
                Id = Guid.NewGuid(),
                Name = "Maria Inativa",
                Phone = "2",
                IsActive = false,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.QueryPatients("Maria", 0, 10, CancellationToken.None);

        Assert.Equal(1, result.TotalItems);
        Assert.Equal("Maria Ativa", result.Items.Single().Name);
    }

    [Fact]
    public async Task QueryPatients_ShouldIncludeDeactivated_WhenRequested()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        context.Patients.AddRange(
            new Patient
            {
                Id = Guid.NewGuid(),
                Name = "Maria Ativa",
                Phone = "1",
                IsActive = true,
            },
            new Patient
            {
                Id = Guid.NewGuid(),
                Name = "Maria Inativa",
                Phone = "2",
                IsActive = false,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.QueryPatients(
            "Maria",
            0,
            10,
            CancellationToken.None,
            includeDeactivated: true
        );

        Assert.Equal(2, result.TotalItems);
    }

    [Fact]
    public async Task SetPatientActive_ShouldDeactivate_WhenThereAreNoBlockers()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var patientId = Guid.NewGuid();
        context.Patients.Add(
            new Patient
            {
                Id = patientId,
                Name = "Paciente",
                Phone = "1",
                IsActive = true,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.SetPatientActive(patientId, false, CancellationToken.None);

        Assert.True(result.Success);
        var patient = await context.Patients.FindAsync(patientId);
        Assert.NotNull(patient);
        Assert.False(patient.IsActive);
    }

    [Fact]
    public async Task SetPatientActive_ShouldBlockDeactivation_WhenPatientBelongsToCouple()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var patientIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        context.Patients.AddRange(
            new Patient
            {
                Id = patientIds[0],
                Name = "Parceiro 1",
                Phone = "1",
            },
            new Patient
            {
                Id = patientIds[1],
                Name = "Parceiro 2",
                Phone = "2",
            }
        );
        context.Couples.Add(
            new Couple
            {
                Id = Guid.NewGuid(),
                Name = "Casal",
                Patient1Id = patientIds[0],
                Patient2Id = patientIds[1],
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.SetPatientActive(patientIds[0], false, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("casal", result.Message);
        var patient = await context.Patients.FindAsync(patientIds[0]);
        Assert.NotNull(patient);
        Assert.True(patient.IsActive);
    }

    [Fact]
    public async Task SetPatientActive_ShouldBlockDeactivation_WhenThereIsFutureScheduledAppointment()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var patientId = Guid.NewGuid();
        context.Patients.Add(
            new Patient
            {
                Id = patientId,
                Name = "Paciente",
                Phone = "1",
            }
        );
        context.Appointments.Add(
            new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                AppointmentDate = DateTimeOffset.UtcNow.AddDays(2),
                AppointmentStatus = AppointmentStatusEnum.Marcada,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.SetPatientActive(patientId, false, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("consultas futuras", result.Message);
    }

    [Fact]
    public async Task SetPatientActive_ShouldBlockDeactivation_WhenThereIsActivePackageWithBalance()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var patientId = Guid.NewGuid();
        context.Patients.Add(
            new Patient
            {
                Id = patientId,
                Name = "Paciente",
                Phone = "1",
            }
        );
        context.AppointmentPackages.Add(
            new AppointmentPackage
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                TotalAppointments = 10,
                RemainingAppointments = 3,
                Status = PackageStatus.Ativo,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.SetPatientActive(patientId, false, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("pacote", result.Message);
    }

    [Fact]
    public async Task SetPatientActive_ShouldReactivatePatient()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var patientId = Guid.NewGuid();
        context.Patients.Add(
            new Patient
            {
                Id = patientId,
                Name = "Paciente",
                Phone = "1",
                IsActive = false,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.SetPatientActive(patientId, true, CancellationToken.None);

        Assert.True(result.Success);
        var patient = await context.Patients.FindAsync(patientId);
        Assert.NotNull(patient);
        Assert.True(patient.IsActive);
    }

    [Fact]
    public async Task SetPatientActive_ShouldReturnFalse_WhenPatientDoesNotExist()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var result = await handler.SetPatientActive(Guid.NewGuid(), false, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Paciente não encontrado.", result.Message);
    }

    [Fact]
    public async Task GetUpcomingBirthdays_ShouldReturnTodayAndUpcomingBirthdays()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var today = TodayPortoVelho();
        var inFiveDays = today.AddDays(5);

        var todayPatientId = Guid.NewGuid();
        var upcomingPatientId = Guid.NewGuid();

        context.Patients.AddRange(
            new Patient
            {
                Id = todayPatientId,
                Name = "Aniversariante Hoje",
                Phone = "1",
                BirthDate = BirthDateForMonthDay(today),
            },
            new Patient
            {
                Id = upcomingPatientId,
                Name = "Aniversariante Futuro",
                Phone = "2",
                BirthDate = BirthDateForMonthDay(inFiveDays),
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.GetUpcomingBirthdays(5, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(todayPatientId, result[0].PatientId);
        Assert.True(result[0].IsToday);
        Assert.Equal(0, result[0].DaysUntil);
        Assert.Equal(upcomingPatientId, result[1].PatientId);
        Assert.False(result[1].IsToday);
        Assert.Equal(5, result[1].DaysUntil);
    }

    [Fact]
    public async Task GetUpcomingBirthdays_ShouldHandleYearTurnover()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var today = TodayPortoVelho();
        var expectedNextBirthday =
            today.Month == 1 && today.Day == 1
                ? new DateOnly(today.Year, 1, 1)
                : new DateOnly(today.Year + 1, 1, 1);

        var patientId = Guid.NewGuid();
        context.Patients.Add(
            new Patient
            {
                Id = patientId,
                Name = "Ano Novo",
                Phone = "1",
                BirthDate = new DateOnly(2000, 1, 1),
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.GetUpcomingBirthdays(400, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(patientId, item.PatientId);
        Assert.Equal(expectedNextBirthday, item.NextBirthday);
        Assert.Equal(expectedNextBirthday.DayNumber - today.DayNumber, item.DaysUntil);
    }

    [Fact]
    public async Task GetUpcomingBirthdays_ShouldHandleFebruary29()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var today = TodayPortoVelho();

        DateOnly Candidate(int year)
        {
            return DateTime.IsLeapYear(year)
                ? new DateOnly(year, 2, 29)
                : new DateOnly(year, 2, 28);
        }

        var expected = Candidate(today.Year);
        if (expected < today)
            expected = Candidate(today.Year + 1);

        var patientId = Guid.NewGuid();
        context.Patients.Add(
            new Patient
            {
                Id = patientId,
                Name = "Nascido em Fevereiro",
                Phone = "1",
                BirthDate = new DateOnly(2000, 2, 29),
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.GetUpcomingBirthdays(400, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(patientId, item.PatientId);
        Assert.Equal(new DateOnly(2000, 2, 29), item.BirthDate);
        Assert.Equal(expected, item.NextBirthday);
        Assert.Equal(expected.DayNumber - today.DayNumber, item.DaysUntil);
    }

    [Fact]
    public async Task GetUpcomingBirthdays_ShouldExcludeDeactivatedPatients()
    {
        await using var context = GetDatabaseContext();
        var handler = GetHandler(context);

        var today = TodayPortoVelho();
        var patientId = Guid.NewGuid();

        context.Patients.Add(
            new Patient
            {
                Id = patientId,
                Name = "Inativo",
                Phone = "1",
                IsActive = false,
                BirthDate = BirthDateForMonthDay(today),
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.GetUpcomingBirthdays(5, CancellationToken.None);

        Assert.Empty(result);
    }
}
