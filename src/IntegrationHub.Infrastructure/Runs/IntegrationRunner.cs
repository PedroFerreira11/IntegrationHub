using System.Net.Http.Json;
using IntegrationHub.Application.Contracts.Runs;
using IntegrationHub.Application.Logging;
using IntegrationHub.Application.Runs;
using IntegrationHub.Domain.Entities;
using IntegrationHub.Domain.Enums;
using IntegrationHub.Infrastructure.Logging;
using IntegrationHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IntegrationHub.Infrastructure.Runs;

public sealed class IntegrationRunner : IIntegrationRunner
{
    private readonly IntegrationHubDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RunLogService _logService;

    public IntegrationRunner(
        IntegrationHubDbContext db,
        IHttpClientFactory httpClientFactory,
        RunLogService logService)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logService = logService;
    }

    public async Task<RunResult> RunAsync(Guid integrationId, CancellationToken ct)
    {
        var integration = await _db.Integrations
            .Include(i => i.SourceEndpoint)
            .Include(i => i.TargetEndpoint)
            .FirstOrDefaultAsync(i => i.Id == integrationId, ct);

        if (integration is null)
            throw new InvalidOperationException("Integration not found");

        if (!integration.IsActive)
            throw new InvalidOperationException("Integration not active");

        if (!integration.SourceEndpoint.IsActive)
            throw new InvalidOperationException("SourceEndpoint not active");

        if (!integration.TargetEndpoint.IsActive)
            throw new InvalidOperationException("TargetEndpoint not active");

        var run = new IntegrationRun
        {
            IntegrationId = integration.Id,
            Status = RunStatus.Pending,
            StartedAt = DateTimeOffset.UtcNow
        };

        _db.IntegrationRuns.Add(run);
        await _db.SaveChangesAsync(ct);

        var http = _httpClientFactory.CreateClient("integration-client");
        var buffer = new RunLogBuffer(run.Id);

        try
        {
            buffer.Info("Run Started");

            var sourceUrl = $"{integration.SourceEndpoint.BaseUrl.TrimEnd('/')}/api/orders";
            buffer.Info($"Fetching orders from: {sourceUrl}");

            var orders = await http.GetFromJsonAsync<List<OrderDto>>(sourceUrl, ct)
                         ?? new List<OrderDto>();

            buffer.Info($"Fetched {orders.Count} orders");

            var targetUrl = $"{integration.TargetEndpoint.BaseUrl.TrimEnd('/')}/api/orders";
            buffer.Info($"Sending orders to: {targetUrl}");

            var response = await http.PostAsJsonAsync(targetUrl, orders, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                buffer.Error($"Target returned {(int)response.StatusCode}: {responseBody}");
                throw new InvalidOperationException($"Target returned {(int)response.StatusCode}");
            }

            buffer.Info($"Target OK: {responseBody}");

            run.Status = RunStatus.Success;
            run.FinishedAt = DateTimeOffset.UtcNow;

            buffer.Info("Run Finished successfully");

            await _db.SaveChangesAsync(ct);
            await _logService.AppendAsync(buffer.Logs, ct);

            return new RunResult(run.Id, run.Status.ToString());
        }
        catch (Exception ex)
        {
            run.Status = RunStatus.Failed;
            run.FinishedAt = DateTimeOffset.UtcNow;
            run.ErrorMessage = ex.Message;

            await _db.SaveChangesAsync(ct);

            buffer.Error($"Run failed: {ex.Message}");
            await _logService.AppendAsync(buffer.Logs, ct);

            throw;
        }
    }
}