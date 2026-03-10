using landing_page_isis;
using landing_page_isis.Authentication;
using landing_page_isis.Components;
using landing_page_isis.core.Interfaces;
using landing_page_isis.Data;
using landing_page_isis.Handlers;
using landing_page_isis.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using RazorLight;

DotNetEnv.Env.Load(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"));

var builder = WebApplication.CreateBuilder(args);

// Configure Portuguese (Brazil) localization
var culture = new System.Globalization.CultureInfo("pt-BR");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// MudBlazor Configuration
builder.Services.AddMudServices();
builder.Services.AddScoped<IsisTheme>();

// Handlers Configuration
builder.Services.AddScoped<IAppointmentHandler, AppointmentHandler>();
builder.Services.AddScoped<IPacientHandler, PacientHandler>();
builder.Services.AddScoped<ILeadHandler, LeadHandler>();
builder.Services.AddScoped<IAuthHandler, AuthHandler>();
builder.Services.AddHostedService<LeadsCleaningService>();

// Email Service Configuration
builder.Services.AddHttpClient(
    "resend",
    client =>
    {
        client.BaseAddress = new Uri("https://api.resend.com/");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                Environment.GetEnvironmentVariable("RESEND_API_KEY") ?? ""
            );
    }
);

builder.Services.AddHostedService<EmailService>();

builder.Services.AddSingleton<RazorLightEngine>(sp =>
    new RazorLightEngineBuilder()
        .UseFileSystemProject(Path.Combine(Directory.GetCurrentDirectory(), "Templates"))
        .UseMemoryCachingProvider()
        .Build()
);

builder.Services.AddHostedService<EmailService>();

// Authentication Configuration
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, AuthProvider>();

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// Authentication Configuration
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
