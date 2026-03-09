using Microsoft.AspNetCore.Mvc;
using SourceApi.Contract;

namespace SourceApi.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public OrdersController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public ActionResult<List<SampleOrderDto>> Get()
    {
        var expectedApiKey = _configuration["ApiKeySettings:ApiKey"];
        var headerName = _configuration["ApiKeySettings:HeaderName"];

        if (string.IsNullOrWhiteSpace(headerName) || string.IsNullOrWhiteSpace(expectedApiKey))
        {
            return StatusCode(500, "API key configuration is missing.");
        }

        if (!Request.Headers.TryGetValue(headerName, out var providedApiKey))
        {
            return Unauthorized();
        }

        if (providedApiKey != expectedApiKey)
        {
            return Unauthorized();
        }

        var now = DateTimeOffset.UtcNow;

        var orders = new List<SampleOrderDto>
        {
            new(1, "Pedro", 39.99m, now.AddMinutes(-30)),
            new(2, "John", 29.99m, now.AddMinutes(-20)),
            new(3, "Nick", 19.99m, now.AddMinutes(-10))
        };

        return Ok(orders);
    }
}