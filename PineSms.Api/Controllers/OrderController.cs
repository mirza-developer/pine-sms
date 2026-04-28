using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PineSms.Api.Auth;
using PineSms.Core.Contracts;
using PineSms.Core.Features.Order;

namespace PineSms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderService orderService;

    public OrderController(IOrderService orderService)
    {
        this.orderService = orderService;
    }

    /// <summary>
    /// Notify PineSms of a new or updated customer order.
    /// Authenticate using the X-Api-Key header with a valid API key.
    /// </summary>
    [HttpPost("notify")]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
    public async Task<IActionResult> Notify([FromBody] NotifyOrderCommand command)
    {
        var result = await orderService.NotifyOrder(command);
        if (!result.Success)
            return BadRequest(new { result.Message });
        return Ok(result);
    }

    [HttpGet("statuses")]
    [Authorize]
    public async Task<IActionResult> GetStatuses()
    {
        var statuses = await orderService.GetAllOrderStatuses();
        return Ok(statuses);
    }

    [HttpPost("statuses")]
    [Authorize]
    public async Task<IActionResult> UpsertStatus([FromBody] UpsertOrderStatusCommand command)
    {
        var (success, message) = await orderService.UpsertOrderStatus(command);
        if (!success) return BadRequest(new { message });
        return Ok(new { message });
    }

    [HttpDelete("statuses/{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteStatus(int id)
    {
        var (success, message) = await orderService.DeleteOrderStatus(id);
        if (!success) return BadRequest(new { message });
        return Ok(new { message });
    }
}
