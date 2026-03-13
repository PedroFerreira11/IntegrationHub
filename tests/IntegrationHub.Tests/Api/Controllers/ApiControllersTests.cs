using IntegrationHub.Api.Contracts.Endpoints;
using IntegrationHub.Api.Contracts.Integrations;
using IntegrationHub.Api.Controllers;
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
}
