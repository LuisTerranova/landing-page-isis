using landing_page_isis.core.Interfaces;

namespace landing_page_isis.Services;

/// <summary>
/// Background service that periodically cleans old or processed leads from the database.
/// Runs once every 24 hours.
/// </summary>
public class LeadsCleaningService(IServiceProvider services, ILogger<LeadsCleaningService> logger)
    : BackgroundService
{
    // Execution interval for the cleanup process (once every 24 hours).
    private readonly TimeSpan _period = TimeSpan.FromHours(24);

    /// <summary>
    /// Executes the background cleanup task on a daily schedule using a PeriodicTimer.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Run immediately when the service starts
        await DoWork(ct);

        using PeriodicTimer timer = new(_period);

        // Wait for the next tick (every 24 hours) and perform the cleanup again
        while (await timer.WaitForNextTickAsync(ct))
        {
            await DoWork(ct);
        }
    }

    /// <summary>
    /// Resolves the Lead Handler dependency in a scoped context and triggers the leads cleaning logic.
    /// </summary>
    private async Task DoWork(CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Starting leads cleaning at {Time}", DateTimeOffset.Now);

            // Create a temporary DI scope to resolve the scoped ILeadHandler service
            using var scope = services.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ILeadHandler>();
            var result = await handler.CleanLeads(ct);

            logger.LogInformation(
                "Leads cleaned successfully at {Time}. {Message}",
                DateTimeOffset.Now,
                result.Message
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cleaning leads at {Time}", DateTimeOffset.Now);
        }
    }
}
