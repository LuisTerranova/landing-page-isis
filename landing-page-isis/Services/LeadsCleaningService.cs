using landing_page_isis.core.Interfaces;

namespace landing_page_isis.Services;

public class LeadsCleaningService(IServiceProvider services, ILogger<LeadsCleaningService> logger)
    : BackgroundService
{
    private readonly TimeSpan _period = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using PeriodicTimer timer = new(_period);

        while (await timer.WaitForNextTickAsync(ct))
        {
            await DoWork(ct);
        }
    }

    private async Task DoWork(CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Starting leads cleaning at {Time}", DateTimeOffset.Now);

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
