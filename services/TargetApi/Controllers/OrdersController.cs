using Microsoft.AspNetCore.Mvc;
using TargetApi.Contracts;

namespace TargetApi.Controllers;


[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    [HttpPost]
    public ActionResult Receive([FromBody] List<ReceiveOrderDto> orders)
    {
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