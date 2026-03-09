using IntegrationHub.Application.Runs;
using IntegrationHub.Domain.Entities;
using IntegrationHub.Domain.Enums;
using IntegrationHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IntegrationHub.Api.Background;

public class IntegrationRunWorker : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ErrorDelay = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IntegrationRunWorker> _logger;

    public IntegrationRunWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<IntegrationRunWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var processed = await TryProcessNextRunAsync(ct);

                if (!processed)
                    await Task.Delay(IdleDelay, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected worker error");
                await Task.Delay(ErrorDelay, ct);
            }
        }
    }

    private async Task<bool> TryProcessNextRunAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<IntegrationHubDbContext>();
        var runner = scope.ServiceProvider.GetRequiredService<IIntegrationRunner>();

        var run = await GetNextRunnableRunAsync(db, ct);

        if (run is null)
            return false;

        await MarkAsRunningAsync(db, run, ct);

        try
        {
            await runner.RunAsync(run.Id, ct);
            await MarkAsSuccessAsync(db, run, ct);
        }
        catch (Exception ex)
        {
            await HandleRunFailureAsync(db, run, ex, ct);
        }

        return true;
    }

    private static async Task<IntegrationRun?> GetNextRunnableRunAsync(
        IntegrationHubDbContext db,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        return await db.IntegrationRuns
            .Where(r =>
                r.Status == RunStatus.Pending &&
                (r.NextRetryAt == null || r.NextRetryAt <= now))
            .OrderBy(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    private static async Task MarkAsRunningAsync(
        IntegrationHubDbContext db,
        IntegrationRun run,
        CancellationToken ct)
    {
        run.Status = RunStatus.Running;
        run.StartedAt = DateTimeOffset.UtcNow;
        run.ErrorMessage = null;

        await db.SaveChangesAsync(ct);
    }

    private static async Task MarkAsSuccessAsync(
        IntegrationHubDbContext db,
        IntegrationRun run,
        CancellationToken ct)
    {
        run.Status = RunStatus.Success;
        run.FinishedAt = DateTimeOffset.UtcNow;
        run.ErrorMessage = null;
        run.NextRetryAt = null;

        await db.SaveChangesAsync(ct);
    }

    private async Task HandleRunFailureAsync(
        IntegrationHubDbContext db,
        IntegrationRun run,
        Exception ex,
        CancellationToken ct)
    {
        run.RetryCount++;

        if (CanRetry(run))
        {
            ScheduleRetry(run, ex);

            _logger.LogWarning(
                ex,
                "Run {RunId} failed on attempt {Attempt}. Retrying at {NextRetryAt}",
                run.Id,
                run.RetryCount,
                run.NextRetryAt);
        }
        else
        {
            MarkAsFailed(run, ex);

            _logger.LogError(
                ex,
                "Run {RunId} failed permanently after {RetryCount} attempts",
                run.Id,
                run.RetryCount);
        }

        await db.SaveChangesAsync(ct);
    }

    private static bool CanRetry(IntegrationRun run)
    {
        return run.RetryCount <= run.MaxRetries;
    }

    private static void ScheduleRetry(IntegrationRun run, Exception ex)
    {
        var delay = GetRetryDelay(run.RetryCount);

        run.Status = RunStatus.Pending;
        run.NextRetryAt = DateTimeOffset.UtcNow.Add(delay);
        run.ErrorMessage = ex.Message;
        run.FinishedAt = null;
    }

    private static void MarkAsFailed(IntegrationRun run, Exception ex)
    {
        run.Status = RunStatus.Failed;
        run.FinishedAt = DateTimeOffset.UtcNow;
        run.NextRetryAt = null;
        run.ErrorMessage = ex.Message;
    }

    private static TimeSpan GetRetryDelay(int retryCount)
    {
        return retryCount switch
        {
            1 => TimeSpan.FromSeconds(10),
            2 => TimeSpan.FromSeconds(30),
            3 => TimeSpan.FromMinutes(1),
            _ => TimeSpan.FromMinutes(2)
        };
    }
}