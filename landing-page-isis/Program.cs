using landing_page_isis;
using landing_page_isis.Authentication;
using landing_page_isis.Components;
using landing_page_isis.core.Interfaces;
using landing_page_isis.Data;
using landing_page_isis.Handlers;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

DotNetEnv.Env.Load(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"));

var builder = WebApplication.CreateBuilder(args);

// Configure Portuguese (Brazil) localization
var culture = new System.Globalization.CultureInfo("pt-BR");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;

//Force http, omarchy's fault perhaps.
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenLocalhost(5000);
});

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddScoped<IsisTheme>();
builder.Services.AddScoped<IAppointmentHandler, AppointmentHandler>();
builder.Services.AddScoped<IPacientHandler, PacientHandler>();
builder.Services.AddScoped<ILeadHandler, LeadHandler>();
builder.Services.AddScoped<IAuthHandler, AuthHandler>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, AuthProvider>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder
    .Services.AddAuthentication("Cookies")
    .AddCookie(
        "Cookies",
        options =>
        {
            options.LoginPath = "/admin/login";
            options.AccessDeniedPath = "/acesso-negado";
            options.ExpireTimeSpan = TimeSpan.FromDays(3);
        }
    );

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await app.SeedAdmin();
app.Run();
