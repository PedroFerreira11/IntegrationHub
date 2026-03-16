using Microsoft.AspNetCore.Mvc;
using SourceApi.Contract;

namespace SourceApi.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public CustomersController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public ActionResult<List<SampleCustomerDto>> Get()
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

        var customers = new List<SampleCustomerDto>
        {
            new(101, "Pedro Duarte", "pedro@example.com", now.AddDays(-30)),
            new(102, "John Smith", "john@example.com", now.AddDays(-20)),
            new(103, "Nick Carter", "nick@example.com", now.AddDays(-10))
        };

        return Ok(customers);
    }
}
