using IntegrationHub.Application.Logging;
using IntegrationHub.Domain.Entities;
using IntegrationHub.Domain.Enums;

namespace IntegrationHub.Application.Processors;

public interface IIntegrationProcessor
{
    IntegrationType SupportedType { get; }

    Task ProcessAsync(
        IntegrationRun run,
        HttpClient http,
        RunLogBuffer buffer,
        CancellationToken ct);
}