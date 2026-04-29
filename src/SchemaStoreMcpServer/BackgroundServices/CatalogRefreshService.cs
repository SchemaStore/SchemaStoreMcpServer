using SchemaStoreMcpServer.Services;

namespace SchemaStoreMcpServer.BackgroundServices;

/// <summary>
/// Background service that refreshes the schema catalog at startup and every 30 minutes.
/// </summary>
public sealed class CatalogRefreshService(ISchemaCatalogService catalogService, ILogger<CatalogRefreshService> logger) : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(12);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial load
        logger.LogInformation("Loading schema catalog on startup...");
        await catalogService.RefreshAsync(stoppingToken);

        // Periodic refresh
        using var timer = new PeriodicTimer(RefreshInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            logger.LogInformation("Refreshing schema catalog...");
            await catalogService.RefreshAsync(stoppingToken);
        }
    }
}
