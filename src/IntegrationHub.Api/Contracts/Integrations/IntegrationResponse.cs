namespace IntegrationHub.Api.Contracts.Integrations;

public sealed record IntegrationResponse(
    Guid Id,
    string Name,
    Guid SourceEndpointId,
    string SourceEndpointName,
    Guid TargetEndpointId,
    string TargetEndpointName,
    bool IsActive
    );