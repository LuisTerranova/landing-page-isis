using landing_page_isis.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;

namespace landing_page_isis.Extensions;

/// <summary>
/// Provides extension methods for application security policies, authentication, proxy headers, and HSTS setup.
/// </summary>
public static class SecurityExtensions
{
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

        // Forwarded Headers configuration to resolve scheme and client IPs correctly behind proxies (Cloudflare, Nginx)
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // Trust Docker network and standard private IP ranges
            options.KnownIPNetworks.Add(
                new System.Net.IPNetwork(System.Net.IPAddress.Parse("172.16.0.0"), 12)
            );
            options.KnownIPNetworks.Add(
                new System.Net.IPNetwork(System.Net.IPAddress.Parse("10.0.0.0"), 8)
            );
            options.KnownIPNetworks.Add(
                new System.Net.IPNetwork(System.Net.IPAddress.Parse("192.168.0.0"), 16)
            );
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
}
