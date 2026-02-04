using landing_page_isis.core;

namespace landing_page_isis.Data;

public static class DatabaseSeed
{
    public static async Task SeedAdmin(this IHost app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            if (!context.Users.Any())
                return;

            var email = configuration["ADMIN_EMAIL"];
            var password = configuration["ADMIN_PASSWORD"];
            var name = configuration["ADMIN_NAME"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return;

            var admin = new User
            {
                Email = email,
                Name = name,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            };

            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}