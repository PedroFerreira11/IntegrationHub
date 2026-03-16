using System.Net;
using System.Net.Http.Json;
using IntegrationHub.Application.Contracts.Runs;
using IntegrationHub.Application.Logging;
using IntegrationHub.Domain.Entities;
using IntegrationHub.Domain.Enums;
using IntegrationHub.Infrastructure.Processors;

namespace IntegrationHub.Tests.Infrastructure.Processors;

public class CustomersIntegrationProcessorTests
{
    [Fact]
    public async Task ProcessAsync_WhenEndpointsAreConfigured_FetchesMapsAndPostsCustomers()
    {
        List<TargetCustomerDto>? postedCustomers = null;

        var handler = new StubHttpMessageHandler(async request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                Assert.Equal("https://source.test/api/customers", request.RequestUri?.ToString());
                Assert.True(request.Headers.TryGetValues("x-api-key", out var sourceValues));
                Assert.Equal("source-key", sourceValues.Single());

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new List<SourceCustomerDto>
                    {
                        new(1, "Alice Johnson", "alice@example.com", DateTimeOffset.UtcNow),
                        new(2, "Bob Stone", "bob@example.com", DateTimeOffset.UtcNow)
                    })
                };
            }

            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://target.test/api/customers", request.RequestUri?.ToString());
            Assert.True(request.Headers.TryGetValues("x-api-key", out var targetValues));
            Assert.Equal("target-key", targetValues.Single());

            postedCustomers = await request.Content!.ReadFromJsonAsync<List<TargetCustomerDto>>();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { received = 2, firstCustomerId = 1 })
            };
        });

        var processor = new CustomersIntegrationProcessor();
        var run = CreateRun();
        var httpClient = new HttpClient(handler);
        var buffer = new RunLogBuffer(run.Id);

        await processor.ProcessAsync(run, httpClient, buffer, CancellationToken.None);

        Assert.NotNull(postedCustomers);
        Assert.Collection(
            postedCustomers!,
            customer =>
            {
                Assert.Equal(1, customer.CustomerId);
                Assert.Equal("Alice Johnson", customer.FullName);
                Assert.Equal("alice@example.com", customer.Email);
            },
            customer =>
            {
                Assert.Equal(2, customer.CustomerId);
                Assert.Equal("Bob Stone", customer.FullName);
                Assert.Equal("bob@example.com", customer.Email);
            });
    }

    [Fact]
    public async Task ProcessAsync_WhenTargetReturnsError_ThrowsInvalidOperationException()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new List<SourceCustomerDto>
                    {
                        new(1, "Alice Johnson", "alice@example.com", DateTimeOffset.UtcNow)
                    })
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("invalid payload")
            });
        });

        var processor = new CustomersIntegrationProcessor();
        var run = CreateRun();
        var httpClient = new HttpClient(handler);
        var buffer = new RunLogBuffer(run.Id);

        var action = () => processor.ProcessAsync(run, httpClient, buffer, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Equal("Target returned 400", exception.Message);
    }

    private static IntegrationRun CreateRun()
    {
        return new IntegrationRun
        {
            Id = Guid.NewGuid(),
            Integration = new Integration
            {
                Id = Guid.NewGuid(),
                Name = "Customers sync",
                Type = IntegrationType.Customers,
                SourceEndpoint = new SystemEndpoint
                {
                    Id = Guid.NewGuid(),
                    Name = "Source",
                    BaseUrl = "https://source.test",
                    ApiKeyHeaderName = "x-api-key",
                    ApiKey = "source-key"
                },
                TargetEndpoint = new SystemEndpoint
                {
                    Id = Guid.NewGuid(),
                    Name = "Target",
                    BaseUrl = "https://target.test",
                    ApiKeyHeaderName = "x-api-key",
                    ApiKey = "target-key"
                }
            }
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request);
        }
    }
}
