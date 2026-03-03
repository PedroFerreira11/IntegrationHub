namespace DefaultNamespace;

public class Integration
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "";

    public Guid SourceEndPointId { get; set; }
    
    public Guid TargetEndpointId { get; set; }

    public bool IsActive { get; set; } = true;
}