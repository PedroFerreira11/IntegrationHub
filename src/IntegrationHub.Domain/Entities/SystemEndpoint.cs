namespace IntegrationHub.Domain.Entities;

public class SystemEndpoint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string? ApiKey { get; set; }
    public bool IsActive { get; set; } = true;
    
    public List<Integration> SourceIntegrations { get; set; } = new();
    public List<Integration> TargetIntegrations { get; set; } = new();
}