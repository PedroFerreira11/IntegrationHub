namespace IntegrationHub.Api.Contracts.Endpoints;

public sealed record CreateEndpointRequest(
    string Name,
    string BaseUrl,
    string? ApiKey,
    bool IsActive= true
    );