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

app.MapGet(
    "/health",
    async (AppDbContext db) =>
    {
        try
        {
            await db.Database.CanConnectAsync();
            return Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
        }
        catch
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
);

await app.SeedAdmin();

app.Run();
