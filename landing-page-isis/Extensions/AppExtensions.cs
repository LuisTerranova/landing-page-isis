using landing_page_isis.Components;

namespace landing_page_isis.Extensions;

public static class AppExtensions
{
    public static void ConfigurePipeline(this WebApplication app)
    {
        app.UseForwardedHeaders();

        // HTTP Security Headers
        app.Use(async (ctx, next) =>
        {
            ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            ctx.Response.Headers.Append("X-Frame-Options", "DENY");
            ctx.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            ctx.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
            await next();
        });

        if (!app.Environment.IsDevelopment())
        {
            app.UseResponseCompression();
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        
        app.UseRateLimiter();
        
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
    }
}
