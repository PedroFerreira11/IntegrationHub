using IntegrationHub.Application.Logging;
using IntegrationHub.Application.Processors;
using IntegrationHub.Domain.Entities;
using IntegrationHub.Domain.Enums;
using IntegrationHub.Infrastructure.Processors;

namespace IntegrationHub.Tests.Infrastructure.Processors;

public class IntegrationProcessorResolverTests
{
    [Fact]
    public void Resolve_WhenProcessorExists_ReturnsProcessor()
    {
        // Arrange
        var processor = new FakeProcessor(IntegrationType.Orders);
        var resolver = new IntegrationProcessorResolver(new[] { processor });

        // Act
        var result = resolver.Resolve(IntegrationType.Orders);

        // Assert
        Assert.Same(processor, result);
    }
    
    [Fact]
    public void Resolve_WhenProcessorDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var processor = new FakeProcessor(IntegrationType.Orders);
        var resolver = new IntegrationProcessorResolver(new[] { processor });

        // Act
        var action = () => resolver.Resolve(IntegrationType.Customers);

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("Customers", exception.Message);
    }
    
    private sealed class FakeProcessor : IIntegrationProcessor
    {
        public FakeProcessor(IntegrationType supportedType)
        {
            SupportedType = supportedType;
        }

        public IntegrationType SupportedType { get; }

        public Task ProcessAsync(
            IntegrationRun run,
            HttpClient http,
            RunLogBuffer buffer,
            CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
