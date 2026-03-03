using IntegrationHub.Domain.Enums;

namespace IntegrationHub.Domain.Entities;

public class IntegrationRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid IntegrationId { get; set; }
    public Integration Integration { get; set; } = null!;

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }

    public RunStatus Status { get; set; } = RunStatus.Pending;

    public string? ErrorMessage { get; set; }

    public List<IntegrationLog> Logs { get; set; } = new();
}