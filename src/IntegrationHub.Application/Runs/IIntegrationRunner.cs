namespace IntegrationHub.Application.Runs;

public interface IIntegrationRunner
{
    public Task<RunResult> RunAsync(Guid integrationId, CancellationToken ct);
}