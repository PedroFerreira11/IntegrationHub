using IntegrationHub.Application.Logging;
using IntegrationHub.Application.Processors;
using IntegrationHub.Application.Runs;
using IntegrationHub.Domain.Entities;
using IntegrationHub.Infrastructure.Logging;
using IntegrationHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IntegrationHub.Infrastructure.Runs;

public sealed class IntegrationRunner : IIntegrationRunner
{
    private readonly IntegrationHubDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RunLogService _logService;
    private readonly IIntegrationProcessorResolver _processorResolver;

    public IntegrationRunner(
        IntegrationHubDbContext db,
        IHttpClientFactory httpClientFactory,
        RunLogService logService,
        IIntegrationProcessorResolver processorResolver)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logService = logService;
        _processorResolver = processorResolver;
    }

    public async Task RunAsync(Guid runId, CancellationToken ct)
    {
        var run = await GetRunAsync(runId, ct);
        ValidateIntegration(run.Integration);
        
        var http = _httpClientFactory.CreateClient("integration-client");
        var buffer = new RunLogBuffer(run.Id);
        
        try
        {
            buffer.Info("Run started");
            
            var processor = _processorResolver.Resolve(run.Integration.Type);
            await processor.ProcessAsync(run, http, buffer, ct);
            
            buffer.Info("Run finished successfully");
        
            await FlushLogsAsync(buffer, ct);
        }
        catch (Exception ex)
        {
            buffer.Error($"Run failed: {ex.Message}");
            await FlushLogsAsync(buffer, ct);
            throw;
        }
    }

    private async Task<IntegrationRun> GetRunAsync(Guid runId, CancellationToken ct)
    {
        var run = await _db.IntegrationRuns
            .Include(r => r.Integration)
                .ThenInclude(i => i.SourceEndpoint)
            .Include(r => r.Integration)
                .ThenInclude(i => i.TargetEndpoint)
            .FirstOrDefaultAsync(r => r.Id == runId, ct);

        if (run is null)
            throw new InvalidOperationException("Run not found");

        return run;
    }

    private static void ValidateIntegration(Integration integration)
    {
        if (!integration.IsActive)
            throw new InvalidOperationException("Integration not active");

        if (!integration.SourceEndpoint.IsActive)
            throw new InvalidOperationException("Source endpoint not active");

        if (!integration.TargetEndpoint.IsActive)
            throw new InvalidOperationException("Target endpoint not active");
    }

    private async Task FlushLogsAsync(RunLogBuffer logBuffer, CancellationToken ct)
    {
        await _logService.AppendAsync(logBuffer.Logs, ct);
    }
}