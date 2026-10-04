using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

public sealed class TireViewsOptions
{
    public double IntervalMinutes { get; set; } = 60;
}

// Runs inside the API process; no separate hosting service is required.
sealed class TireViewsWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<TireViewsOptions> options,
    ILogger<TireViewsWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromMinutes(options.Value.IntervalMinutes));

        try
        {
            // Refresh immediately after startup, including when Render wakes the API.
            do
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApiDb>();
                    var updatedAt = DateTimeOffset.UtcNow;

                    // PostgreSQL evaluates random() separately for each row. Updating
                    // only these columns avoids overwriting concurrent CRUD edits.
                    var count = await db.Database.ExecuteSqlInterpolatedAsync($"""
                        UPDATE "Tires"
                        SET "ViewsLastHour" = floor(random() * 51)::integer,
                            "ViewsUpdatedAt" = {updatedAt}
                        """, stoppingToken);

                    logger.LogInformation("Updated simulated view counts for {Count} tires at {UpdatedAt}.",
                        count, updatedAt);
                }
                catch (Exception exception) when (
                    exception is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(exception, "Could not update simulated tire views; retrying next interval.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
    }
}
