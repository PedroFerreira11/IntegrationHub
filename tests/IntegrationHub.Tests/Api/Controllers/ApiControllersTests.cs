using IntegrationHub.Api.Contracts.Endpoints;
using IntegrationHub.Api.Contracts.Integrations;
using IntegrationHub.Api.Controllers;
using IntegrationHub.Application.Runs;
using IntegrationHub.Domain.Entities;
using IntegrationHub.Domain.Enums;
using IntegrationHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationHub.Tests.Api.Controllers;

public class EndpointsControllerTests
{
    [Fact]
    public async Task GetAll_WhenEndPointsExist_ReturnsOrderedList()
    {
        var options = TestDb.CreateOptions();
        await using var db = new IntegrationHubDbContext(options);
        var controller = new EndpointsController(db);
        
        var endpointB = TestDb.CreateSystemEndpoint(db, "Api B");
        await db.SaveChangesAsync();
        
        var endpointA = TestDb.CreateSystemEndpoint(db, "Api A");
        await db.SaveChangesAsync();
        
        var result = await controller.GetAll(CancellationToken.None);
        
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<List<EndpointResponse>>(ok.Value);
        
        Assert.Equal(2, response.Count);
        Assert.Equal("Api A", response[0].Name);
        Assert.Equal("Api B", response[1].Name);
    }
    
    [Fact]
    public async Task GetById_WhenEndPointExists_ReturnsOk()
    {
        var options = TestDb.CreateOptions();
        await using var db = new IntegrationHubDbContext(options);
        
        var endpoint = TestDb.CreateSystemEndpoint(db);
        await db.SaveChangesAsync();
        
        var controller = new EndpointsController(db);
        
        var result = await controller.GetById(endpoint.Id, CancellationToken.None);
        
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<EndpointResponse>(ok.Value);
        
        Assert.Equal(endpoint.Id, response.Id);
        Assert.Equal(endpoint.Name, response.Name);
        Assert.Equal(endpoint.BaseUrl, response.BaseUrl);
        Assert.True(response.HasApiKey);
        Assert.Equal(endpoint.ApiKeyHeaderName, response.ApiKeyHeaderName);
        Assert.True(response.IsActive);
    }
    
    [Fact]
    public async Task GetById_WhenEndpointDoesNotExist_ReturnsNotFound()
    {
        var options = TestDb.CreateOptions();

        await using var db = new IntegrationHubDbContext(options);
        var controller = new EndpointsController(db);
        
        var endpointId = Guid.NewGuid();
        
        var result = await controller.GetById(endpointId, CancellationToken.None);
        
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_WhenEndpointIsValid_PersistsEndpointAndReturnsCreated()
    {  
        var options = TestDb.CreateOptions();
        await using var db = new IntegrationHubDbContext(options);
        var controller = new EndpointsController(db);

        var request = new CreateEndpointRequest(
            "Source API",
            "https://source.local/",
            "secret",
            "x-api-key",
            true);

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<EndpointResponse>(created.Value);

        Assert.Equal(nameof(EndpointsController.GetById), created.ActionName);
        Assert.Equal("Source API", response.Name);
        Assert.Equal("https://source.local", response.BaseUrl);
        Assert.True(response.HasApiKey);

        var saved = await db.SystemEndpoints.SingleAsync();
        Assert.Equal("Source API", saved.Name);
        Assert.Equal("https://source.local", saved.BaseUrl);
        Assert.Equal("secret", saved.ApiKey);
        Assert.Equal("x-api-key", saved.ApiKeyHeaderName);
    }
    
    [Fact]
    public async Task Create_WhenApiKeyConfigIsIncomplete_ReturnsBadRequest()
    {  
        var options = TestDb.CreateOptions();
        await using var db = new IntegrationHubDbContext(options);
        var controller = new EndpointsController(db);

        var request = new CreateEndpointRequest(
            "Source API",
            "https://source.local",
            "secret",
            null,
            true);

        var result = await controller.Create(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("ApiKey e ApiKeyHeaderName can't be null or empty.", badRequest.Value);
        Assert.Empty(db.SystemEndpoints);
    }
}

public class IntegrationsControllersTests
{
    [Fact]
    public async Task GetAll_WhenIntegrationsExist_ReturnsOrderedList()
    {
        var options = TestDb.CreateOptions();
        await using var db = new IntegrationHubDbContext(options);
        var controller = new IntegrationsController(db);

        var sourceB = TestDb.CreateSystemEndpoint(db, "Source B");
        var targetB = TestDb.CreateSystemEndpoint(db, "Target B");
        var sourceA = TestDb.CreateSystemEndpoint(db, "Source A");
        var targetA = TestDb.CreateSystemEndpoint(db, "Target A");
        await db.SaveChangesAsync();

        db.Integrations.AddRange(
            new Integration
            {
                Name = "Integration B",
                Type = IntegrationType.Orders,
                SourceEndpointId = sourceB.Id,
                TargetEndpointId = targetB.Id,
                IsActive = true
            },
            new Integration
            {
                Name = "Integration A",
                Type = IntegrationType.Orders,
                SourceEndpointId = sourceA.Id,
                TargetEndpointId = targetA.Id,
                IsActive = true
            });
        await db.SaveChangesAsync();

        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<List<IntegrationResponse>>(ok.Value);

        Assert.Equal(2, response.Count);
        Assert.Equal("Integration A", response[0].Name);
        Assert.Equal("Integration B", response[1].Name);
        Assert.Equal("Source A", response[0].SourceEndpointName);
        Assert.Equal("Target A", response[0].TargetEndpointName);
    }

    [Fact]
    public async Task GetById_WhenIntegrationExists_ReturnsOk()
    {
        var options = TestDb.CreateOptions();
        await using var db = new IntegrationHubDbContext(options);
        var controller = new IntegrationsController(db);

        var source = TestDb.CreateSystemEndpoint(db, "Source");
        var target = TestDb.CreateSystemEndpoint(db, "Target");
        await db.SaveChangesAsync();

        var integration = new Integration
        {
            Name = "Integration",
            Type = IntegrationType.Orders,
            SourceEndpointId = source.Id,
            TargetEndpointId = target.Id,
            IsActive = true
        };

        db.Integrations.Add(integration);
        await db.SaveChangesAsync();

        var result = await controller.GetById(integration.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<IntegrationResponse>(ok.Value);

        Assert.Equal(integration.Id, response.Id);
        Assert.Equal("Integration", response.Name);
        Assert.Equal(IntegrationType.Orders, response.Type);
        Assert.Equal(source.Id, response.SourceEndpointId);
        Assert.Equal(target.Id, response.TargetEndpointId);
        Assert.Equal("Source", response.SourceEndpointName);
        Assert.Equal("Target", response.TargetEndpointName);
        Assert.True(response.IsActive);
    }

    [Fact]
    public async Task GetById_WhenIntegrationDoesNotExist_ReturnsNotFound()
    {
        var options = TestDb.CreateOptions();
        await using var db = new IntegrationHubDbContext(options);
        var controller = new IntegrationsController(db);

        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_WhenRequestIsValid_PersistsIntegrationAndReturnsCreated()
    {
        var options = TestDb.CreateOptions();
        await using var db = new IntegrationHubDbContext(options);
        var integrationsController = new IntegrationsController(db);
        
        var endpointA =  TestDb.CreateSystemEndpoint(db, "Api A");
        var endpointB = TestDb.CreateSystemEndpoint(db, "Api B");
        await db.SaveChangesAsync();

        var integration = new CreateIntegrationRequest(
            "Integration",
            IntegrationType.Orders,
            endpointA.Id,
            endpointB.Id,
            true
            );
        
        var result = await integrationsController.Create(integration, CancellationToken.None);
        
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<IntegrationResponse>(created.Value);
        
        Assert.Equal(nameof(IntegrationsController.GetById), created.ActionName);
        Assert.Equal("Integration", response.Name);
        Assert.Equal(IntegrationType.Orders, response.Type);
        Assert.Equal(endpointA.Id, response.SourceEndpointId);
        Assert.Equal(endpointB.Id, response.TargetEndpointId);
        Assert.True(response.IsActive);
        
        var saved = await db.Integrations.SingleAsync();
        Assert.Equal("Integration", saved.Name);
        Assert.Equal(IntegrationType.Orders, saved.Type);
        Assert.Equal(endpointA.Id, saved.SourceEndpointId);
        Assert.Equal(endpointB.Id, saved.TargetEndpointId);
    }

    [Fact]
    public async Task Create_WhenSourceAndTargetAreEqual_ReturnsBadRequest()
    {
        var options = TestDb.CreateOptions();
        await using var db = new IntegrationHubDbContext(options);
        var integrationsController = new IntegrationsController(db);
        
        var endpointA = TestDb.CreateSystemEndpoint(db, "Api A");
        await db.SaveChangesAsync();
        
        var integration = new CreateIntegrationRequest(
            "Integration",
            IntegrationType.Orders,
            endpointA.Id,
            endpointA.Id,
            true
        );
        var result = await integrationsController.Create(integration, CancellationToken.None);
        
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Source and Target can't be the same endpoint!", badRequest.Value);
        
        Assert.Empty(db.Integrations);
    } 
    
    [Fact]
    public async Task Create_WhenSourceEndpointDoesNotExist_ReturnsBadRequest()
    {
        var options = TestDb.CreateOptions();
        await using var db = new IntegrationHubDbContext(options);
        var integrationsController = new IntegrationsController(db);
        
        var endpointA = TestDb.CreateSystemEndpoint(db, "Api A");
        await db.SaveChangesAsync();
        
        var integration = new CreateIntegrationRequest(
            "Integration",
            IntegrationType.Orders,
            Guid.NewGuid(),
            endpointA.Id,
            true
        );
        var result = await integrationsController.Create(integration, CancellationToken.None);
        
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("SourceEndpoint doesn't exist!", badRequest.Value);
        
        Assert.Empty(db.Integrations);
    }
    
    [Fact]
    public async Task Create_WhenTargetEndpointDoesNotExist_ReturnsBadRequest()
    {
        var options = TestDb.CreateOptions();
        await using var db = new IntegrationHubDbContext(options);
        var integrationsController = new IntegrationsController(db);
        
        var endpointA = TestDb.CreateSystemEndpoint(db, "Api A");
        await db.SaveChangesAsync();
        
        var integration = new CreateIntegrationRequest(
            "Integration",
            IntegrationType.Orders,
            endpointA.Id,
            Guid.NewGuid(),
            true
        );
        var result = await integrationsController.Create(integration, CancellationToken.None);
        
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("TargetEndpoint doesn't exist!", badRequest.Value);
        
        Assert.Empty(db.Integrations);
    }
}

public class RunsControllerTests
{
    [Fact]
    public async Task CreateRun_WhenIntegrationExists_PersistsPendingRunAndReturnsAccepted()
    {
        var options = TestDb.CreateOptions();
        await using var db = new IntegrationHubDbContext(options);
        
        var integration = await TestDb.AddIntegrationAsync(db, "Integration", IntegrationType.Orders);
        var controller = new RunsController(new TestDb.FakeIntegrationRunner(), db);
        
        var result = await controller.CreateRun(integration.Id, CancellationToken.None);
        
        var accepted =  Assert.IsType<AcceptedResult>(result);
        Assert.Equal(integration.Id, TestObjectReader.GetProperty<Guid>(accepted.Value!, "IntegrationId"));
        Assert.Equal("Pending", TestObjectReader.GetProperty<string>(accepted.Value!, "Status"));
        
        var saved = await db.IntegrationRuns.SingleAsync();
        Assert.Equal(integration.Id, saved.IntegrationId);
        Assert.Equal(RunStatus.Pending, saved.Status);
        Assert.Equal(0, saved.RetryCount);
        Assert.Equal(3, saved.MaxRetries);
    }

    [Fact]
    public async Task CreateRun_WhenIntegrationDoesNotExists_ReturnsNotFound()
    {
        var options = TestDb.CreateOptions();
        await using var db = new IntegrationHubDbContext(options);
        
        var controller = new RunsController(new TestDb.FakeIntegrationRunner(), db);
        
        var integrationId = Guid.NewGuid();
        var result = await controller.CreateRun(integrationId, CancellationToken.None);
        
        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Empty(db.IntegrationRuns);
    }
    
}

internal static class TestDb
{
    public static DbContextOptions<IntegrationHubDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<IntegrationHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public static SystemEndpoint CreateSystemEndpoint(IntegrationHubDbContext db, string name= "SourceApi")
    {
        var endpoint = new SystemEndpoint
        {
            Name = name,
            BaseUrl = "https://source.local",
            ApiKey = "secret",
            ApiKeyHeaderName = "x-api-key",
            IsActive = true
        };
        
        db.SystemEndpoints.Add(endpoint);
        return endpoint;
    }

    public static async Task<Integration> AddIntegrationAsync(
        IntegrationHubDbContext db,
        string name,
        IntegrationType type)
    {
        var source = CreateSystemEndpoint(db,"Source");
        var target = CreateSystemEndpoint(db,"Target" );
        await db.SaveChangesAsync();
         
        var integration = new Integration
        {
            Name = name,
            Type = type,
            SourceEndpoint = source,
            TargetEndpoint = target,
            IsActive = true
        };

        db.Integrations.Add(integration);
        await db.SaveChangesAsync();

        return integration;
    }
    
    public sealed class FakeIntegrationRunner : IIntegrationRunner
    {
        public Task RunAsync(Guid runId, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}

internal static class TestObjectReader
{
    public static T GetProperty<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName);
        Assert.NotNull(property);

        var value = property.GetValue(instance);
        Assert.IsType<T>(value);

        return (T)value;
    }
}
