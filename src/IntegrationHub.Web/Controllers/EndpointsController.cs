using IntegrationHub.Web.Models.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationHub.Web.Controllers;

public class EndpointsController : Controller
{
    private readonly IHttpClientFactory _client;
    private readonly string _endpointsUrl = "endpoints";
    
    public EndpointsController(IHttpClientFactory client)
    {
        _client = client;
    }

    public async Task<IActionResult> Index()
    {
        var client = _client.CreateClient("MyApi");
        
        var endpoints = await client.GetFromJsonAsync<List<EndpointDto>>(_endpointsUrl);
        
        return View(endpoints);
    }

    public async Task<IActionResult> Create()
    {
        var model = new CreateEndpointDto();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateEndpointDto model)
    {
        var client = _client.CreateClient("MyApi");

        if (!ModelState.IsValid)
            return View(model);

        var request = new
        {
            model.Name,
            model.BaseUrl,
            model.ApiKey,
            model.ApiKeyHeaderName,
            model.IsActive
        };

        var response = await client.PostAsJsonAsync(_endpointsUrl, request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", error);
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }
}