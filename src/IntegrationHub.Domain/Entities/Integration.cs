using IntegrationHub.Domain.Enums;

namespace IntegrationHub.Domain.Entities;

public class Integration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public IntegrationType Type  { get; set; }
    public Guid SourceEndpointId { get; set; }
    public Guid TargetEndpointId { get; set; }
    
    public SystemEndpoint SourceEndpoint { get; set; } = null!;
    public SystemEndpoint TargetEndpoint { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public List<IntegrationRun> Runs { get; set; } = new();
}