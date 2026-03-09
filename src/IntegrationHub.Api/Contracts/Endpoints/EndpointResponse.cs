namespace IntegrationHub.Api.Contracts.Endpoints;

public sealed record EndpointResponse(
    Guid Id,
    string Name,
    string BaseUrl,
    bool HasApiKey,
    string? ApiKeyHeaderName,
    bool IsActive
    );