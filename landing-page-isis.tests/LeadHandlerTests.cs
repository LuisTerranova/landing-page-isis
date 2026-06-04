using landing_page_isis.core;
using landing_page_isis.core.Models;
using landing_page_isis.Handlers;
using landing_page_isis.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.tests;

public class LeadHandlerTests
{
    private AppDbContext GetDatabaseContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection, contextOwnsConnection: true)
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private LeadHandler CreateHandler(AppDbContext context)
    {
        return new LeadHandler(context);
    }

    [Fact]
    public async Task CreateLead_ShouldReturnTrue_WhenValid()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            Name = "Maria Silva",
            Email = "maria@email.com",
            Phone = "(11) 99999-8888",
            Intent = "Quero agendar uma consulta.",
        };

        var result = await handler.CreateLead(lead);

        Assert.True(result.Success);
        var saved = await context.Leads.FindAsync(lead.Id);
        Assert.NotNull(saved);
        Assert.Equal("Maria Silva", saved.Name);
    }

    [Fact]
    public async Task CreateLead_ShouldReturnFalse_WhenNull()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var result = await handler.CreateLead(null!);

        Assert.False(result.Success);
        Assert.Equal("Dados não podem ser nulos.", result.Message);
    }

    [Fact]
    public async Task CreateLead_ShouldFormatPhone()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            Name = "Carlos",
            Email = "carlos@email.com",
            Phone = "(11) 91234-5678",
            Intent = "Teste",
        };

        await handler.CreateLead(lead);

        var saved = await context.Leads.FindAsync(lead.Id);
        Assert.NotNull(saved);
        Assert.Equal("11912345678", saved.Phone);
    }

    [Fact]
    public async Task GetLeads_ShouldReturnPaginatedList()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        for (int i = 0; i < 7; i++)
        {
            context.Leads.Add(
                new Lead
                {
                    Id = Guid.NewGuid(),
                    Name = $"Lead {i}",
                    Email = $"lead{i}@email.com",
                    Phone = "11999999999",
                    Intent = "Interesse",
                    Created = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i)),
                }
            );
        }
        await context.SaveChangesAsync();

        var result = await handler.GetLeads(0, 3, CancellationToken.None);

        Assert.Equal(7, result.TotalItems);
        Assert.Equal(3, result.Items.Count());
    }

    [Fact]
    public async Task GetLeads_ShouldReturnEmpty_WhenNone()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var result = await handler.GetLeads(0, 10, CancellationToken.None);

        Assert.Equal(0, result.TotalItems);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetLead_ShouldReturnLead_WhenExists()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var id = Guid.NewGuid();
        context.Leads.Add(
            new Lead
            {
                Id = id,
                Name = "Ana",
                Email = "ana@email.com",
                Phone = "11999999999",
                Intent = "Quero informações.",
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.GetLead(id);

        Assert.NotNull(result);
        Assert.Equal("Ana", result.Name);
    }

    [Fact]
    public async Task GetLead_ShouldReturnNull_WhenNotFound()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var result = await handler.GetLead(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task ApproveLead_ShouldCreatePatient_WhenLeadExists()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var leadId = Guid.NewGuid();
        context.Leads.Add(
            new Lead
            {
                Id = leadId,
                Name = "João",
                Email = "joao@email.com",
                Phone = "11987654321",
                Intent = "Quero marcar.",
                PolicySigned = true,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.ApproveLead(leadId);

        Assert.True(result.Success);
        var dbLead = await context.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.Id == leadId);
        Assert.NotNull(dbLead);
        Assert.Equal(LeadStatusEnum.Aprovado, dbLead.LeadStatus);

        var patient = await context.Patients.FirstOrDefaultAsync(p => p.Name == "João");
        Assert.NotNull(patient);
        Assert.Equal("joao@email.com", patient.Email);
        Assert.True(patient.PolicySigned);
    }

    [Fact]
    public async Task ApproveLead_ShouldReturnFalse_WhenLeadNotFound()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var result = await handler.ApproveLead(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("Lead não encontrado.", result.Message);
    }

    [Fact]
    public async Task DeleteLead_ShouldReturnTrue_WhenExists()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var id = Guid.NewGuid();
        context.Leads.Add(
            new Lead
            {
                Id = id,
                Name = "Delete Me",
                Email = "delete@email.com",
                Phone = "11999999999",
                Intent = "Test",
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.DeleteLead(id);

        Assert.True(result.Success);
        Assert.Null(await context.Leads.FindAsync(id));
    }

    [Fact]
    public async Task DeleteLead_ShouldReturnFalse_WhenNotFound()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var result = await handler.DeleteLead(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("Lead nao encontrado.", result.Message);
    }

    [Fact]
    public async Task CleanLeads_ShouldMarkOldLeadsAsExpired()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        context.Leads.AddRange(
            new Lead
            {
                Id = Guid.NewGuid(),
                Name = "Novo",
                Email = "a@a.com",
                Phone = "11999999999",
                Intent = "",
                Created = DateOnly.FromDateTime(DateTime.UtcNow),
                LeadStatus = LeadStatusEnum.Novo,
            },
            new Lead
            {
                Id = Guid.NewGuid(),
                Name = "Velho",
                Email = "b@b.com",
                Phone = "11999999999",
                Intent = "",
                Created = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20)),
                LeadStatus = LeadStatusEnum.Novo,
            }
        );
        await context.SaveChangesAsync();

        var result = await handler.CleanLeads(CancellationToken.None);

        Assert.True(result.Success);
        var velho = await context.Leads.AsNoTracking().FirstAsync(l => l.Name == "Velho");
        Assert.Equal(LeadStatusEnum.Expirado, velho.LeadStatus);
        var novo = await context.Leads.AsNoTracking().FirstAsync(l => l.Name == "Novo");
        Assert.Equal(LeadStatusEnum.Novo, novo.LeadStatus);
    }

    [Fact]
    public async Task CleanLeads_ShouldDeleteVeryOldExpiredLeads()
    {
        await using var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        context.Leads.AddRange(
            new Lead
            {
                Id = Guid.NewGuid(),
                Name = "Old",
                Email = "old@a.com",
                Phone = "11999999999",
                Intent = "",
                Created = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-4)),
                LeadStatus = LeadStatusEnum.Expirado,
            },
            new Lead
            {
                Id = Guid.NewGuid(),
                Name = "Recent",
                Email = "recent@a.com",
                Phone = "11999999999",
                Intent = "",
                Created = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
                LeadStatus = LeadStatusEnum.Expirado,
            }
        );
        await context.SaveChangesAsync();

        await handler.CleanLeads(CancellationToken.None);

        Assert.Null(await context.Leads.FirstOrDefaultAsync(l => l.Name == "Old"));
        Assert.NotNull(await context.Leads.FirstOrDefaultAsync(l => l.Name == "Recent"));
    }

    [Fact]
    public void GetWhatsAppUrl_ShouldGenerateCorrectUrl()
    {
        var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var lead = new Lead { Name = "Maria", Phone = "(11) 91234-5678" };

        var url = handler.GetWhatsAppUrl(lead);

        Assert.StartsWith("https://wa.me/5511912345678?text=", url);
        Assert.Contains("Maria", url);
        Assert.Contains("Tudo", url);
    }

    [Fact]
    public void GetWhatsAppUrl_ShouldReturnEmpty_WhenNoPhone()
    {
        var context = GetDatabaseContext();
        var handler = CreateHandler(context);

        var lead = new Lead { Name = "Test", Phone = "" };

        var url = handler.GetWhatsAppUrl(lead);

        Assert.Equal(string.Empty, url);
    }
}
