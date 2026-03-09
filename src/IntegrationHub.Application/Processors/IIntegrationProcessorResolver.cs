using IntegrationHub.Domain.Enums;

namespace IntegrationHub.Application.Processors;

public interface IIntegrationProcessorResolver
{
    IIntegrationProcessor Resolve(IntegrationType integrationType);
}