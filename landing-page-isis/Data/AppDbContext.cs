using System.Reflection;
using landing_page_isis.core;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<Pacient> Pacients { get; set; }
    public DbSet<Lead> Leads { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}