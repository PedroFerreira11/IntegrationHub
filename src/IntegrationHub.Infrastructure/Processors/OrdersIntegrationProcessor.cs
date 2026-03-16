using System.Net.Http.Json;
using IntegrationHub.Application.Contracts.Runs;
using IntegrationHub.Application.Logging;
using IntegrationHub.Application.Processors;
using IntegrationHub.Domain.Entities;
using IntegrationHub.Domain.Enums;

namespace IntegrationHub.Infrastructure.Processors;

public sealed class OrdersIntegrationProcessor : IIntegrationProcessor
{
    private const string OrdersPath = "/api/orders";
    
    public IntegrationType SupportedType => IntegrationType.Orders;

    public async Task ProcessAsync(
        IntegrationRun run,
        HttpClient http,
        RunLogBuffer buffer,
        CancellationToken ct)
    {
        var orders = await FetchOrdersAsync(http, run, buffer, ct);

        var mappedOrders  = MapOrders(orders, buffer);
        
        await SendOrdersAsync(http, run, mappedOrders, buffer, ct);
    }

    private static string BuildOrdersUrl(string baseUrl)
    {
        return $"{baseUrl.TrimEnd('/')}{OrdersPath}";
    }

    private static async Task<List<SourceOrderDto>> FetchOrdersAsync(
        HttpClient http,
        IntegrationRun run,
        RunLogBuffer buffer,
        CancellationToken ct)
    {
        var sourceUrl = BuildOrdersUrl(run.Integration.SourceEndpoint.BaseUrl);
        buffer.Info($"Fetching orders from: {sourceUrl}");

        using var request = new HttpRequestMessage(HttpMethod.Get, sourceUrl);
        ApplyApiKeyHeader(request, run.Integration.SourceEndpoint);

        buffer.Info(
            $"Source auth config - Header: {run.Integration.SourceEndpoint.ApiKeyHeaderName}, HasKey: {!string.IsNullOrWhiteSpace(run.Integration.SourceEndpoint.ApiKey)}");
        using var response = await http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            buffer.Error($"Source returned {(int)response.StatusCode}: {errorBody}");
            throw new InvalidOperationException($"Source returned {(int)response.StatusCode}");
        }

        var orders = await response.Content.ReadFromJsonAsync<List<SourceOrderDto>>(cancellationToken: ct)
                     ?? new List<SourceOrderDto>();

        buffer.Info($"Fetched {orders.Count} orders");

        return orders;
    }

    private static async Task SendOrdersAsync(
        HttpClient http,
        IntegrationRun run,
        List<TargetOrderDto> orders,
        RunLogBuffer buffer,
        CancellationToken ct)
    {
        var targetUrl = BuildOrdersUrl(run.Integration.TargetEndpoint.BaseUrl);
        buffer.Info($"Sending {orders.Count} orders to: {targetUrl}");
        
        using var request = new HttpRequestMessage(HttpMethod.Post, targetUrl)
        {
            Content = JsonContent.Create(orders)
        };
        
        ApplyApiKeyHeader(request, run.Integration.TargetEndpoint);
        
        using var response = await http.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            buffer.Error($"Target returned {(int)response.StatusCode}: {responseBody}");
            throw new InvalidOperationException($"Target returned {(int)response.StatusCode}");
        }

        buffer.Info($"Target OK: {responseBody}");
    }

    private static List<TargetOrderDto> MapOrders(List<SourceOrderDto> sourceOrders, RunLogBuffer buffer)
    {
        buffer.Info($"Mapping {sourceOrders.Count} orders");
        
        return sourceOrders
            .Select(o => new TargetOrderDto(
                o.OrderId,
                o.CustomerName,
                o.Total
            ))
            .ToList();
    }
    
    private static void ApplyApiKeyHeader(HttpRequestMessage request, SystemEndpoint endpoint)
    {
        var hasApiKey = !string.IsNullOrWhiteSpace(endpoint.ApiKey);
        var hasHeaderName = !string.IsNullOrWhiteSpace(endpoint.ApiKeyHeaderName);

        if (hasApiKey && hasHeaderName)
        {
            var headerName = endpoint.ApiKeyHeaderName!;
            var apiKey = endpoint.ApiKey!;
            request.Headers.TryAddWithoutValidation(headerName, apiKey);
        }
    }
}
