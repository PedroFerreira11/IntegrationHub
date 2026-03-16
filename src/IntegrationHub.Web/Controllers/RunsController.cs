using IntegrationHub.Web.Models.Runs;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationHub.Web.Controllers;

public class RunsController : Controller
{
    private readonly IHttpClientFactory _client;
    
    public RunsController(IHttpClientFactory client)
    {
        _client = client;
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var client = _client.CreateClient("MyApi");

        var run = await client.GetFromJsonAsync<RunDetailsResponseDto>($"runs/{id}", ct);

        if (run is null)
            return NotFound();

        return View(run);
    }

    public async Task<IActionResult> Logs(Guid id, CancellationToken ct)
    {
        var client = _client.CreateClient("MyApi");

        var logs = await client.GetFromJsonAsync<List<RunLogResponseDto>>($"runs/{id}/logs", ct);

        if (logs is null) return NotFound();
        
        return Json(logs);
    }
    
    public async Task<IActionResult> Status(Guid id, CancellationToken ct)
    {
        var client = _client.CreateClient("MyApi");

        var run = await client.GetFromJsonAsync<RunDetailsResponseDto>($"runs/{id}", ct);

        if (run is null)
            return NotFound();

        return Json(run);
    }
}