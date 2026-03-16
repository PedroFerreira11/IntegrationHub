using IntegrationHub.Web.Models.Endpoints;
using IntegrationHub.Web.Models.Integrations;
using IntegrationHub.Web.Models.Runs;
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Run(Guid id)
    {
        var client = _client.CreateClient("MyApi");

        var response = await client.PostAsync($"integrations/{id}/runs", null);

        if (!response.IsSuccessStatusCode)
        {
            TempData["ErrorMessage"] = "Error running integration";
            return RedirectToAction(nameof(Details), new { id });
        }

        var run = await response.Content.ReadFromJsonAsync<CreateRunResponseDto>();

        if (run is null)
        {
            TempData["ErrorMessage"] = "Run created but returned null";
            return RedirectToAction(nameof(Details), new { id });
        }

        return RedirectToAction("Details", "Runs", new { id = run.Id });
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var client = _client.CreateClient("MyApi");
        var response = await client.DeleteAsync($"{_integrationsUrl}/{id}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            TempData["ErrorMessage"] = string.IsNullOrWhiteSpace(error)
                ? "Error deleting integration."
                : error;
        }

        return RedirectToAction(nameof(Index));
    }
}
