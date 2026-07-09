using landing_page_isis.Components;

namespace landing_page_isis.Extensions;

/// <summary>
/// Provides extension methods to set up and configure the HTTP request pipeline (middleware) of the WebApplication.
/// </summary>
public static class AppExtensions
{
    /// <summary>
    /// Configures the middleware pipeline including security headers, routing, static files, security, and Blazor mapping.
    /// </summary>
    /// <param name="app">The WebApplication instance to configure.</param>
    public static void ConfigurePipeline(this WebApplication app)
    {
        // Handle forwarded headers (client IP, scheme) when behind a reverse proxy (like Cloudflare or Nginx)
        app.UseForwardedHeaders();

        // HTTP Security Headers middleware
        app.Use(
            async (ctx, next) =>
            {
                // Prevent browser MIME type sniffing
                ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                // Mitigate clickjacking attacks by preventing iframe nesting
                ctx.Response.Headers.Append("X-Frame-Options", "DENY");
                // Limit referer information sent to third-party domains
                ctx.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
                // Block browser access to sensitive client features (camera, location, mic)
                ctx.Response.Headers.Append(
                    "Permissions-Policy",
                    "camera=(), microphone=(), geolocation=()"
                );
                await next();
            }
        );

        // Production-only performance and security optimizations
        if (!app.Environment.IsDevelopment())
        {
            app.UseResponseCompression();
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();

        // Enforce rate limiting configurations (defined in BuilderExtensions)
        app.UseRateLimiter();

        // Setup Blazor Authentication, Authorization, and Anti-forgery protections
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        
        // Serve static web assets optimized by .NET 9
        app.MapStaticAssets();
        
        // Map the root App Blazor component and configure Interactive Server rendering mode
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
    }
}
