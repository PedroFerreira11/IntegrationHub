namespace DefaultNamespace;

public class SystemEndpoint
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "";

    public string BaseUrl { get; set; } = "";
    
    public string? ApiKey { get; set; }

    public bool IsActive { get; set; } = true;
}