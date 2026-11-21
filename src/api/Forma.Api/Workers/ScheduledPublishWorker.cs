using Forma.Application.Publishing;

namespace Forma.Api.Workers;

/// <summary>
/// Runs scheduled publishes that are due: once at start (a publish missed while the API was down runs then), and then
/// every <c>Publishing:WorkerInterval</c> (one minute by default).
/// </summary>
public sealed class ScheduledPublishWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<ScheduledPublishWorker> logger) : BackgroundService
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(1);

    public TimeSpan Interval => configuration.GetValue<TimeSpan?>("Publishing:WorkerInterval") is { } interval && interval > TimeSpan.Zero
        ? interval
        : DefaultInterval;

    /// <summary>Runs every pending publish that is due now, and returns how many there were.</summary>
    public async Task<int> RunDueAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IScheduledPublishService>();

        return await service.RunDueAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            await RunSafelyAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunSafelyAsync(CancellationToken stoppingToken)
    {
        try
        {
            var count = await RunDueAsync(stoppingToken);

            if (count > 0)
                logger.LogInformation("Ran {Count} scheduled publish(es).", count);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Running scheduled publishes failed.");
        }
    }
}
