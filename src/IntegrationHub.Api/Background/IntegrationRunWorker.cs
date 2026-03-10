using IntegrationHub.Application.Runs;

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
                using var scope = _scopeFactory.CreateScope();

                var processor = scope.ServiceProvider
                    .GetRequiredService<IIntegrationRunProcessor>();

                var processed = await processor.TryProcessNextRunAsync(ct);

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
}
