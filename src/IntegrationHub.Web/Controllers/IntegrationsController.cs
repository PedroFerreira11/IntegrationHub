using IntegrationHub.Web.Models.Endpoints;
using IntegrationHub.Web.Models.Integrations;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationHub.Web.Controllers;


public class IntegrationsController : Controller
{
    private readonly IHttpClientFactory _client;
    private readonly string _integrationsUrl = "integrations";
    private readonly string _endpointsUrl = "endpoints";
    
    public IntegrationsController(IHttpClientFactory client)
    {
        _client = client;
    }
    
    public async Task<IActionResult> Index()
    {
        var client = _client.CreateClient("MyApi");

        var integrations = await client.GetFromJsonAsync<List<IntegrationResponse>>(_integrationsUrl);
        
        return View(integrations);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var client = _client.CreateClient("MyApi");
        
        var integration = await client.GetFromJsonAsync<IntegrationResponse>(_integrationsUrl+"/"+ id);
        
        return View(integration);
    }

    public async Task<IActionResult> Create()
    {
        var client = _client.CreateClient("MyApi");
        var endpoints = await client.GetFromJsonAsync<List<EndpointDto>>(_endpointsUrl);
        
        var model = new CreateIntegrationViewModel()
        {
            Endpoints = endpoints ?? new List<EndpointDto>()
        };
        
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateIntegrationViewModel model)
    {
        var client = _client.CreateClient("MyApi");

        if (!ModelState.IsValid)
        {
            var endpoints = await client.GetFromJsonAsync<List<EndpointDto>>(_endpointsUrl);
            model.Endpoints = endpoints ?? new List<EndpointDto>();
            return View(model);
        }

        var request = new
        {
            model.Name,
            model.Type,
            model.SourceEndpointId,
            model.TargetEndpointId,
            model.IsActive
        };

        var response = await client.PostAsJsonAsync(_integrationsUrl, request);

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Error Creating Integration");
            var endpoints = await client.GetFromJsonAsync<List<EndpointDto>>(_endpointsUrl);
            model.Endpoints = endpoints ?? new List<EndpointDto>();
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }
}