using landing_page_isis.Data;
using landing_page_isis.Extensions;
using landing_page_isis.Infrastructure.Data;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.AddEnvironmentConfiguration();
builder.AddApplicationSecurity();
builder.AddApplicationServices();
builder.AddDatabaseContext();

var app = builder.Build();

app.ConfigurePipeline();

app.MapGet("/health", async (AppDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            database = "Healthy"
        });
    }
    catch
    {
        return Results.Ok(new
        {
            status = "Degraded",
            timestamp = DateTime.UtcNow,
            database = "Unhealthy"
        });
    }
});

await app.SeedAdmin();

app.Run();
