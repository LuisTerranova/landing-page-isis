using landing_page_isis;
using landing_page_isis.Components;
using landing_page_isis.core.Interfaces;
using landing_page_isis.Data;
using Microsoft.AspNetCore.Connections;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddScoped<IsisTheme>();
builder.Services.AddScoped<IAppointmentHandler, AppointmentHandler>();
builder.Services.AddScoped<IPacientHandler, PacientHandler>();
builder.Services.AddScoped<ILeadHandler, LeadHandler>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/acesso-negado";
        options.ExpireTimeSpan = TimeSpan.FromDays(3);
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}


app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.SeedAdmin();
app.Run();
