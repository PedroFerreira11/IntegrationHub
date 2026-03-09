using Microsoft.AspNetCore.Mvc;
using TargetApi.Contracts;

namespace TargetApi.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public OrdersController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost]
    public ActionResult Receive([FromBody] List<ReceiveOrderDto> orders)
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

        if (orders is null || orders.Count == 0)
        {
            return BadRequest("No orders received");
        }

        return Ok(new
        {
            received = orders.Count,
            firstOrderId = orders.First().OrderId
        });
    }
}