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
        await SendOrdersAsync(http, run, orders, buffer, ct);
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
}