using System.Reflection;
using landing_page_isis.core.ApplicationUser;
using landing_page_isis.core.Models;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Couple> Couples { get; set; }
    public DbSet<Lead> Leads { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<AppointmentRecord> AppointmentRecords { get; set; }
    public DbSet<AppointmentPackage> AppointmentPackages { get; set; }
    public DbSet<Contract> Contracts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
