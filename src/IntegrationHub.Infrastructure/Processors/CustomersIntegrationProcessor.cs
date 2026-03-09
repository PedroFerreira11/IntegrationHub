using System.Net.Http.Json;
using IntegrationHub.Application.Contracts.Runs;
using IntegrationHub.Application.Logging;
using IntegrationHub.Application.Processors;
using IntegrationHub.Domain.Entities;
using IntegrationHub.Domain.Enums;

namespace IntegrationHub.Infrastructure.Processors;

public sealed class CustomersIntegrationProcessor : IIntegrationProcessor
{
    //TODO: change to customers once source and target have that logic done!
    //TODO: change ordersDTO to customers
    private const string CustomersPath = "/api/orders";
    
    public IntegrationType SupportedType => IntegrationType.Customers;

    public async Task ProcessAsync(
        IntegrationRun run, 
        HttpClient http, 
        RunLogBuffer buffer, 
        CancellationToken ct)
    {
        var customers = await FetchCustomersAsync(http, run, buffer, ct);
        await SendCustomersAsync(http, run, customers, buffer, ct);
    }
    
    private static string BuildCustomersUrl(string baseUrl)
    {
        return $"{baseUrl.TrimEnd('/')}{CustomersPath}";
    }

    private static async Task<List<OrderDto>> FetchCustomersAsync(
        HttpClient http,
        IntegrationRun run,
        RunLogBuffer buffer,
        CancellationToken ct)
    {
        var sourceUrl = BuildCustomersUrl(run.Integration.SourceEndpoint.BaseUrl);
        buffer.Info($"Fetching Customers from: {sourceUrl}");

        var customers = await http.GetFromJsonAsync<List<OrderDto>>(sourceUrl, ct)
                     ?? new List<OrderDto>();

        buffer.Info($"Fetched {customers.Count} Customers");

        return customers;
    }

    private static async Task SendCustomersAsync(
        HttpClient http,
        IntegrationRun run,
        List<OrderDto> customers,
        RunLogBuffer buffer,
        CancellationToken ct)
    {
        var targetUrl = BuildCustomersUrl(run.Integration.TargetEndpoint.BaseUrl);
        buffer.Info($"Sending Customers to: {targetUrl}");

        var response = await http.PostAsJsonAsync(targetUrl, customers, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            buffer.Error($"Target returned {(int)response.StatusCode}: {responseBody}");
            throw new InvalidOperationException($"Target returned {(int)response.StatusCode}");
        }

        buffer.Info($"Target OK: {responseBody}");
    }
}