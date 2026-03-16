using Microsoft.AspNetCore.Mvc;
using TargetApi.Contracts;

namespace TargetApi.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public CustomersController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost]
    public ActionResult Receive([FromBody] List<ReceiveCustomerDto> customers)
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

        if (customers is null || customers.Count == 0)
        {
            return BadRequest("No customers received");
        }

        return Ok(new
        {
            received = customers.Count,
            firstCustomerId = customers.First().CustomerId
        });
    }
}
