using System.Net.Http.Json;
using IntegrationHub.Application.Contracts.Runs;
using IntegrationHub.Application.Logging;
using IntegrationHub.Application.Processors;
using IntegrationHub.Domain.Entities;
using IntegrationHub.Domain.Enums;

namespace IntegrationHub.Infrastructure.Processors;

public sealed class CustomersIntegrationProcessor : IIntegrationProcessor
{
    private const string CustomersPath = "/api/customers";

    public IntegrationType SupportedType => IntegrationType.Customers;

    public async Task ProcessAsync(
        IntegrationRun run,
        HttpClient http,
        RunLogBuffer buffer,
        CancellationToken ct)
    {
        var customers = await FetchCustomersAsync(http, run, buffer, ct);

        var mappedCustomers = MapCustomers(customers, buffer);

        await SendCustomersAsync(http, run, mappedCustomers, buffer, ct);
    }

    private static string BuildCustomersUrl(string baseUrl)
    {
        return $"{baseUrl.TrimEnd('/')}{CustomersPath}";
    }

    private static async Task<List<SourceCustomerDto>> FetchCustomersAsync(
        HttpClient http,
        IntegrationRun run,
        RunLogBuffer buffer,
        CancellationToken ct)
    {
        var sourceUrl = BuildCustomersUrl(run.Integration.SourceEndpoint.BaseUrl);
        buffer.Info($"Fetching customers from: {sourceUrl}");

        using var request = new HttpRequestMessage(HttpMethod.Get, sourceUrl);
        ApplyApiKeyHeader(request, run.Integration.SourceEndpoint);

        using var response = await http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            buffer.Error($"Source returned {(int)response.StatusCode}: {errorBody}");
            throw new InvalidOperationException($"Source returned {(int)response.StatusCode}");
        }

        var customers = await response.Content.ReadFromJsonAsync<List<SourceCustomerDto>>(cancellationToken: ct)
                        ?? new List<SourceCustomerDto>();

        buffer.Info($"Fetched {customers.Count} customers");

        return customers;
    }

    private static List<TargetCustomerDto> MapCustomers(List<SourceCustomerDto> sourceCustomers, RunLogBuffer buffer)
    {
        buffer.Info($"Mapping {sourceCustomers.Count} customers");

        return sourceCustomers
            .Select(customer => new TargetCustomerDto(
                customer.CustomerId,
                customer.Name,
                customer.Email
            ))
            .ToList();
    }

    private static async Task SendCustomersAsync(
        HttpClient http,
        IntegrationRun run,
        List<TargetCustomerDto> customers,
        RunLogBuffer buffer,
        CancellationToken ct)
    {
        var targetUrl = BuildCustomersUrl(run.Integration.TargetEndpoint.BaseUrl);
        buffer.Info($"Sending {customers.Count} customers to: {targetUrl}");

        using var request = new HttpRequestMessage(HttpMethod.Post, targetUrl)
        {
            Content = JsonContent.Create(customers)
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
