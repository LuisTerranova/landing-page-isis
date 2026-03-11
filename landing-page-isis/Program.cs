using landing_page_isis.Extensions;
using landing_page_isis.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddEnvironmentConfiguration();
builder.AddApplicationSecurity();
builder.AddApplicationServices();
builder.AddDatabaseContext();

var app = builder.Build();

app.ConfigurePipeline();
await app.SeedAdmin();

app.Run();
