namespace IntegrationHub.Application.Runs;

public interface IIntegrationRunProcessor
{
    Task<bool> TryProcessNextRunAsync(CancellationToken ct);
}