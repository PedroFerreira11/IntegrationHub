using Microsoft.AspNetCore.Mvc;
using SourceApi.Contract;

namespace SourceApi.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    [HttpGet]
    public ActionResult<List<SampleOrderDto>> Get()
    {
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