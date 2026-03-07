namespace IntegrationHub.Application.Runs;

public interface IIntegrationRunner
{
    public Task RunAsync(Guid integrationId, CancellationToken ct);
}