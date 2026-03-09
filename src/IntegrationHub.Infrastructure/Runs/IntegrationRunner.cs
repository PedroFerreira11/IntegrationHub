using System.Net.Http.Json;
using IntegrationHub.Application.Contracts.Runs;
using IntegrationHub.Application.Logging;
using IntegrationHub.Application.Runs;
using IntegrationHub.Domain.Entities;
using IntegrationHub.Infrastructure.Logging;
using IntegrationHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IntegrationHub.Infrastructure.Runs;

public sealed class IntegrationRunner : IIntegrationRunner
{
    private const string OrdersPath = "/api/orders";

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

    public async Task RunAsync(Guid runId, CancellationToken ct)
    {
        var run = await GetRunAsync(runId, ct);
        ValidateIntegration(run.Integration);

        var http = _httpClientFactory.CreateClient("integration-client");
        var buffer = new RunLogBuffer(run.Id);

        try
        {
            buffer.Info("Run started");

            var orders = await FetchOrdersAsync(http, run, buffer, ct);
            await SendOrdersAsync(http, run, orders, buffer, ct);

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

    private static string BuildOrdersUrl(string baseUrl)
    {
        return $"{baseUrl.TrimEnd('/')}{OrdersPath}";
    }

    private static async Task<List<OrderDto>> FetchOrdersAsync(
        HttpClient http,
        IntegrationRun run,
        RunLogBuffer buffer,
        CancellationToken ct)
    {
        var sourceUrl = BuildOrdersUrl(run.Integration.SourceEndpoint.BaseUrl);
        buffer.Info($"Fetching orders from: {sourceUrl}");

        var orders = await http.GetFromJsonAsync<List<OrderDto>>(sourceUrl, ct)
                     ?? new List<OrderDto>();

        buffer.Info($"Fetched {orders.Count} orders");

        return orders;
    }

    private static async Task SendOrdersAsync(
        HttpClient http,
        IntegrationRun run,
        List<OrderDto> orders,
        RunLogBuffer buffer,
        CancellationToken ct)
    {
        var targetUrl = BuildOrdersUrl(run.Integration.TargetEndpoint.BaseUrl);
        buffer.Info($"Sending orders to: {targetUrl}");

        var response = await http.PostAsJsonAsync(targetUrl, orders, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            buffer.Error($"Target returned {(int)response.StatusCode}: {responseBody}");
            throw new InvalidOperationException($"Target returned {(int)response.StatusCode}");
        }

        buffer.Info($"Target OK: {responseBody}");
    }

    private async Task FlushLogsAsync(RunLogBuffer logBuffer, CancellationToken ct)
    {
        await _logService.AppendAsync(logBuffer.Logs, ct);
    }
}