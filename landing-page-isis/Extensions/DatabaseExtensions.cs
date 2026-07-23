using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Extensions;

/// <summary>
/// Provides extension methods for database context setup.
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Sets up the primary PostgreSQL database context via EF Core.
    /// </summary>
    public static void AddDatabaseContext(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
    }
}
