using IntegrationHub.Application.Runs;
using IntegrationHub.Domain.Entities;
using IntegrationHub.Domain.Enums;
using IntegrationHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IntegrationHub.Infrastructure.Runs;

public sealed class IntegrationRunProcessor : IIntegrationRunProcessor
{
    private readonly IntegrationHubDbContext _db;
    private readonly IIntegrationRunner _runner;
    private readonly ILogger<IntegrationRunProcessor> _logger;

    public IntegrationRunProcessor(
        IntegrationHubDbContext db,
        IIntegrationRunner runner,
        ILogger<IntegrationRunProcessor> logger)
    {
        _db = db;
        _runner = runner;
        _logger = logger;
    }

    public async Task<bool> TryProcessNextRunAsync(CancellationToken ct)
    {
        var run = await GetNextRunnableRunAsync(_db, ct);

        if (run is null)
            return false;

        await MarkAsRunningAsync(_db, run, ct);

        try
        {
            await _runner.RunAsync(run.Id, ct);
            await MarkAsSuccessAsync(_db, run, ct);
        }
        catch (Exception ex)
        {
            await HandleRunFailureAsync(_db, run, ex, ct);
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

        if (IntegrationRetryPolicy.CanRetry(run))
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

    private static void ScheduleRetry(IntegrationRun run, Exception ex)
    {
        var delay = IntegrationRetryPolicy.GetRetryDelay(run.RetryCount);

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
}