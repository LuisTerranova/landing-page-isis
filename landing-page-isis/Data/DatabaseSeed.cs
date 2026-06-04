using landing_page_isis.core.ApplicationUser;
using landing_page_isis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace landing_page_isis.Data;

public static class DatabaseSeed
{
    public static async Task SeedAdmin(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        if (context.Users.Any())
        {
            return;
        }

        string? email = null;
        string? password = null;
        string? name = null;

        if (env.IsDevelopment())
        {
            var devConfig = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
                .Build();

            email = devConfig["ADMIN_EMAIL"];
            password = devConfig["ADMIN_PASSWORD"];
            name = devConfig["ADMIN_NAME"];
        }

        email ??= configuration["ADMIN_EMAIL"];
        password ??= configuration["ADMIN_PASSWORD"];
        name ??= configuration["ADMIN_NAME"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Admin email or password not found in configuration");
            return;
        }

        var admin = new User
        {
            Email = email,
            Name = name ?? "Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        };

        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }
}
