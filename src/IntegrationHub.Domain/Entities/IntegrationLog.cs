using IntegrationHub.Domain.Enums;

namespace IntegrationHub.Domain.Entities;

public class IntegrationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid IntegrationRunId { get; set; }
    public IntegrationRun IntegrationRun { get; set; } = null!;

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public LogLevel Level { get; set; } = LogLevel.Info;
    public string Message { get; set; } = "";
}