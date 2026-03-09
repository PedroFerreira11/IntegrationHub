namespace IntegrationHub.Api.Contracts.Endpoints;

public sealed record CreateEndpointRequest(
    string Name,
    string BaseUrl,
    string? ApiKey,
    string? ApiKeyHeaderName,
    bool IsActive= true
    );