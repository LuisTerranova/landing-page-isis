using System.Threading.RateLimiting;
using landing_page_isis.Authentication;
using landing_page_isis.core.Interfaces;
using landing_page_isis.Handlers;
using landing_page_isis.Infrastructure.Data;
using landing_page_isis.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
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
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                }
            );

        builder.Services.AddAuthorization();

        // Rate Limiting Configuration
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status429TooManyRequests;
            
            options.AddPolicy("auth-policy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(5),
                        QueueLimit = 0
                    }));

            options.AddPolicy("lead-policy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        // Forwarded Headers (needed for HTTPS behind Proxy/Cloudflare)
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // Trust Docker network and standard private IP ranges
            options.KnownNetworks.Add(new IPNetwork(System.Net.IPAddress.Parse("172.16.0.0"), 12));
            options.KnownNetworks.Add(new IPNetwork(System.Net.IPAddress.Parse("10.0.0.0"), 8));
            options.KnownNetworks.Add(new IPNetwork(System.Net.IPAddress.Parse("192.168.0.0"), 16));
        });

        // HSTS Configuration
        if (!builder.Environment.IsDevelopment())
        {
            builder.Services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });
        }
    }

    public static void AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();

        // MudBlazor Configuration
        builder.Services.AddMudServices();
        builder.Services.AddMemoryCache();
        builder.Services.AddResponseCompression();
        builder.Services.AddScoped<IsisTheme>();

        // Handlers Configuration
        builder.Services.AddScoped<IAppointmentHandler, AppointmentHandler>();
        builder.Services.AddScoped<IAppointmentRecordHandler, AppointmentRecordHandler>();
        builder.Services.AddScoped<
            IAppointmentRecordExportHandler,
            AppointmentRecordExportHandler
        >();
        builder.Services.AddScoped<IAppointmentPackageHandler, AppointmentPackageHandler>();
        builder.Services.AddScoped<IPatientHandler, PatientHandler>();
        builder.Services.AddScoped<ICoupleHandler, CoupleHandler>();
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
