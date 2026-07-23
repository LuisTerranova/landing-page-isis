using System.Globalization;
using System.Net.Http.Headers;
using FluentValidation;
using landing_page_isis.core.Interfaces;
using landing_page_isis.core.Validators;
using landing_page_isis.Handlers;
using landing_page_isis.Services;
using MudBlazor.Services;
using RazorLight;

namespace landing_page_isis.Extensions;

/// <summary>
/// Provides extension methods for environment configuration, application services, and domain handler DI registrations.
/// </summary>
public static class ServiceCollectionExtensions
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
        var culture = new CultureInfo("pt-BR");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
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

        // Domain Handlers Configuration
        builder.Services.AddDomainHandlers();

        // Register periodic background cleanup worker
        builder.Services.AddHostedService<LeadsCleaningService>();

        // HTTP Client configured to send emails using the Resend service API
        builder.Services.AddHttpClient(
            "resend",
            client =>
            {
                client.BaseAddress = new Uri("https://api.resend.com/");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
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
    /// Registers all domain handlers and FluentValidation validators in the dependency injection container.
    /// </summary>
    public static IServiceCollection AddDomainHandlers(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<PatientValidator>();

        services.AddScoped<IAppointmentHandler, AppointmentHandler>();
        services.AddScoped<IAppointmentRecordHandler, AppointmentRecordHandler>();
        services.AddScoped<IAppointmentRecordExportHandler, AppointmentRecordExportHandler>();
        services.AddScoped<IAppointmentPackageHandler, AppointmentPackageHandler>();
        services.AddScoped<IPatientHandler, PatientHandler>();
        services.AddScoped<ICoupleHandler, CoupleHandler>();
        services.AddScoped<ILeadHandler, LeadHandler>();
        services.AddScoped<IAuthHandler, AuthHandler>();
        services.AddScoped<IContractHandler, ContractHandler>();

        return services;
    }
}
