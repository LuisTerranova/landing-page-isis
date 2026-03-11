using landing_page_isis.Components;

namespace landing_page_isis.Extensions;

public static class AppExtensions
{
    public static void ConfigurePipeline(this WebApplication app)
    {
        app.UseResponseCompression();
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.UseRateLimiter();
        app.MapStaticAssets();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
    }
}
