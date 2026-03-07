using IntegrationHub.Application.Runs;
using IntegrationHub.Domain.Enums;
using IntegrationHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IntegrationHub.Api.Background;

public class IntegrationRunWorker : BackgroundService
{
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
                using var scope = _scopeFactory.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<IntegrationHubDbContext>();
                var runner = scope.ServiceProvider.GetRequiredService<IIntegrationRunner>();

                var run = await db.IntegrationRuns
                    .Where(r => r.Status == RunStatus.Pending)
                    .OrderBy(r => r.CreatedAt)
                    .FirstOrDefaultAsync(ct);

                if (run is null)
                {
                    await Task.Delay(2000, ct);
                    continue;
                }

                run.Status = RunStatus.Running;
                run.StartedAt = DateTime.Now;
                run.ErrorMessage = null;

                await db.SaveChangesAsync(ct);
                
                try
                {
                    await runner.RunAsync(run.Id, ct);
                    
                    run.Status = RunStatus.Success;
                    run.ErrorMessage = null;
                    run.NextRetryAt = null;
                    
                    await db.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    run.RetryCount++;

                    if (run.RetryCount < run.MaxRetries)
                    {
                        var delay = GetRetryDelay(run.RetryCount); 
                        
                        run.Status = RunStatus.Pending;
                        run.NextRetryAt = DateTimeOffset.UtcNow.Add(delay);
                        run.ErrorMessage = ex.Message;
                        run.FinishedAt = null;
                        
                        _logger.LogWarning(
                            ex,
                            "Run {RunId} failed on attempt {Attempt}. Retrying at {NextRetryAt}",
                            run.Id,
                            run.RetryCount,
                            run.NextRetryAt);
                    }
                    else
                    {
                        run.Status = RunStatus.Failed;
                        run.FinishedAt = DateTimeOffset.UtcNow;
                        run.NextRetryAt = null;
                        run.ErrorMessage = ex.Message;
                        
                        _logger.LogError(
                            ex,
                            "Run {RunId} failed permanently after {RetryCount} attempts",
                            run.Id,
                            run.RetryCount);
                    }
                    await db.SaveChangesAsync(ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected worker error");
                await Task.Delay(5000, ct);
            }
        }
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