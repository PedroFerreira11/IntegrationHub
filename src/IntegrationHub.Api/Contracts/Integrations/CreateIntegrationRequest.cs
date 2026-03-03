namespace IntegrationHub.Api.Contracts.Integrations;

public sealed record CreateIntegrationRequest(
    string Name,
    Guid SourceEndpointId,
    Guid TargetEndpointId,
    bool IsActive
    );