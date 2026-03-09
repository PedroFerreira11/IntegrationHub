using IntegrationHub.Application.Processors;
using IntegrationHub.Domain.Enums;

namespace IntegrationHub.Infrastructure.Processors;

public sealed class IntegrationProcessorResolver : IIntegrationProcessorResolver
{
    private readonly IReadOnlyDictionary<IntegrationType, IIntegrationProcessor> _processors;

    public IntegrationProcessorResolver(IEnumerable<IIntegrationProcessor> processors)
    {
        _processors = processors.ToDictionary(p => p.SupportedType);
    }

    public IIntegrationProcessor Resolve(IntegrationType integrationType)
    {
        if (_processors.TryGetValue(integrationType, out var processor))
            return processor;

        throw new InvalidOperationException(
            $"No processor registered for integration type '{integrationType}'.");
    }
}