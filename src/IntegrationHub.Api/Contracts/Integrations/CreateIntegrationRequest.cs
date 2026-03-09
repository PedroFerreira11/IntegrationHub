using IntegrationHub.Domain.Enums;

namespace IntegrationHub.Api.Contracts.Integrations;

public sealed record CreateIntegrationRequest(
    string Name,
    IntegrationType Type,
    Guid SourceEndpointId,
    Guid TargetEndpointId,
    bool IsActive = true
    );