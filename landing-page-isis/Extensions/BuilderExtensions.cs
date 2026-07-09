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

/// <summary>
/// Provides extension methods for configuring application configurations, security policies, dependency injection services, and database contexts during setup.
/// </summary>
public static class BuilderExtensions
{
    /// <summary>
    /// Loads environment variables from the workspace .env file and sets Brazilian Portuguese as the default system culture.
    /// </summary>
    public static void AddEnvironmentConfiguration(this WebApplicationBuilder builder)
    {
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
        if (File.Exists(envPath))
        {
            DotNetEnv.Env.Load(envPath);
        }

        // Configure Portuguese (Brazil) localization to format currency and dates appropriately
        var culture = new System.Globalization.CultureInfo("pt-BR");
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    /// <summary>
    /// Configures application authentication (cookies), rate limiting, proxy header forwarding, and HTTP Strict Transport Security (HSTS).
    /// </summary>
    public static void AddApplicationSecurity(this WebApplicationBuilder builder)
    {
        // Authentication Configuration using cookie providers
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
                    options.ExpireTimeSpan = TimeSpan.FromHours(8); // Cookie session expiration
                }
            );

        builder.Services.AddAuthorization();

        // Fixed-window rate limiting policies to prevent endpoint abuse
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status429TooManyRequests;
            
            // Limit authentication attempts: 5 requests per 5 minutes per IP
            options.AddPolicy("auth-policy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(5),
                        QueueLimit = 0
                    }));

            // Limit lead registration attempts: 3 requests per 1 minute per IP
            options.AddPolicy("lead-policy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            // Limit contract endpoints access: 2 requests per 5 minutes per IP
            options.AddPolicy("contract-policy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 2,
                        Window = TimeSpan.FromMinutes(5),
                        QueueLimit = 0
                    }));
        });

        // Forwarded Headers configuration to resolve scheme and client IPs correctly behind proxies (Cloudflare, Nginx)
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // Trust Docker network and standard private IP ranges
            options.KnownIPNetworks.Add(new System.Net.IPNetwork(System.Net.IPAddress.Parse("172.16.0.0"), 12));
            options.KnownIPNetworks.Add(new System.Net.IPNetwork(System.Net.IPAddress.Parse("10.0.0.0"), 8));
            options.KnownIPNetworks.Add(new System.Net.IPNetwork(System.Net.IPAddress.Parse("192.168.0.0"), 16));
        });

        // HSTS settings to enforce SSL in production environment
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

    /// <summary>
    /// Configures internal Blazor components, third-party libraries (MudBlazor), handlers DI mappings, hosted services, and HTTP clients.
    /// </summary>
    public static void AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();

        // MudBlazor services configuration
        builder.Services.AddMudServices();
        builder.Services.AddMemoryCache();
        builder.Services.AddResponseCompression();
        builder.Services.AddScoped<IsisTheme>();

        // Domain Handlers Configuration (Dependency Injection lifetimes)
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
        builder.Services.AddScoped<IContractHandler, ContractHandler>();
        
        // Register periodic background cleanup worker
        builder.Services.AddHostedService<LeadsCleaningService>();

        // HTTP Client configured to send emails using the Resend service API
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

        // Precompile and cache Razor views dynamically (used inside email body renderings)
        builder.Services.AddSingleton<RazorLightEngine>(sp =>
            new RazorLightEngineBuilder()
                .UseFileSystemProject(Path.Combine(Directory.GetCurrentDirectory(), "Templates"))
                .UseMemoryCachingProvider()
                .Build()
        );

        // Register periodic email reminder worker
        builder.Services.AddHostedService<EmailService>();
    }

    /// <summary>
    /// Sets up the primary PostgreSQL database context via EF Core.
    /// </summary>
    public static void AddDatabaseContext(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
    }
}
