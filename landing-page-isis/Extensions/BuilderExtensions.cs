using System.Threading.RateLimiting;
using landing_page_isis.Authentication;
using landing_page_isis.core.Interfaces;
using landing_page_isis.Data;
using landing_page_isis.Handlers;
using landing_page_isis.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using RazorLight;

namespace landing_page_isis.Extensions;

public static class BuilderExtensions
{
    public static void AddEnvironmentConfiguration(this WebApplicationBuilder builder)
    {
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
        if (File.Exists(envPath))
        {
            DotNetEnv.Env.Load(envPath);
        }

        // Configure Portuguese (Brazil) localization
        var culture = new System.Globalization.CultureInfo("pt-BR");
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    public static void AddApplicationSecurity(this WebApplicationBuilder builder)
    {
        // Rate Limiting Configuration
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter(
                policyName: "contact-form",
                limiterOptions =>
                {
                    limiterOptions.PermitLimit = 3;
                    limiterOptions.Window = TimeSpan.FromMinutes(1);
                    limiterOptions.QueueLimit = 0;
                }
            );
        });

        // Authentication Configuration
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<AuthenticationStateProvider, AuthProvider>();

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
    }

    public static void AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();

        // MudBlazor Configuration
        builder.Services.AddMudServices();
        builder.Services.AddMemoryCache();
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

        builder.Services.AddSingleton<RazorLightEngine>(sp =>
            new RazorLightEngineBuilder()
                .UseFileSystemProject(Path.Combine(Directory.GetCurrentDirectory(), "Templates"))
                .UseMemoryCachingProvider()
                .Build()
        );

        builder.Services.AddHostedService<EmailService>();
    }

    public static void AddDatabaseContext(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
    }
}
