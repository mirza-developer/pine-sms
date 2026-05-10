using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PineSms.Api.Auth;
using PineSms.Api.Queue;
using PineSms.Core.Contracts;
using PineSms.Core.Dtos;
using PineSms.Core.Features.Order;

namespace PineSms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderService orderService;
    private readonly OrderNotifyQueue orderNotifyQueue;

    public OrderController(IOrderService orderService, OrderNotifyQueue orderNotifyQueue)
    {
        this.orderService = orderService;
        this.orderNotifyQueue = orderNotifyQueue;
    }

    /// <summary>
    /// Notify PineSms of a new or updated customer order.
    /// Authenticate using the X-Api-Key header with a valid API key.
    /// </summary>
    [HttpPost("notify")]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
    public IActionResult Notify([FromBody] NotifyOrderCommand command)
    {
        orderNotifyQueue.Writer.TryWrite(command);
        return Ok(new ResponseDto()
        {
            Success = true,
            Message = "Data received and will be processed shortly"
        });
    }

    [HttpGet("statuses")]
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> GetStatuses()
    {
        var statuses = await orderService.GetAllOrderStatuses();
        return Ok(statuses);
    }

    [HttpPost("statuses")]
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> UpsertStatus([FromBody] UpsertOrderStatusCommand command)
    {
        var (success, message) = await orderService.UpsertOrderStatus(command);
        if (!success) return BadRequest(new { message });
        return Ok(new { message });
    }

    [HttpDelete("statuses/{id:int}")]
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> DeleteStatus(int id)
    {
        var (success, message) = await orderService.DeleteOrderStatus(id);
        if (!success) return BadRequest(new { message });
        return Ok(new { message });
    }
}
