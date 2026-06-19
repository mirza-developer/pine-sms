using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PineAI.Api.Auth;
using PineAI.Api.Queue;
using PineAI.Core.Contracts;
using PineAI.Core.Dtos;
using PineAI.Core.Features.Order;

namespace PineAI.Api.Controllers;

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

    [HttpGet("track/{orderCode}")]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
    public async Task<IActionResult> Track(string orderCode)
    {
        var result = await orderService.GetOrderByCode(orderCode);
        if (!result.Found)
            return NotFound(new ResponseDto { Success = false, Message = "سفارشی با این کد یافت نشد" });
        return Ok(result);
    }

    /// <summary>
    /// Notify PineAI of a new or updated customer order.
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

    [HttpPost("bulk-tracking")]
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> BulkUpdateTracking([FromBody] BulkUpdateTrackingCommand command)
    {
        var result = await orderService.BulkUpdateTracking(command);
        return Ok(result);
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
        var result = await orderService.UpsertOrderStatus(command);
        if (!result.Success) return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    [HttpDelete("statuses/{id:int}")]
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> DeleteStatus(int id)
    {
        var result = await orderService.DeleteOrderStatus(id);
        if (!result.Success) return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    [HttpGet("statistics")]
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> GetStatistics([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string groupBy = "day")
    {
        var query = new GetOrderStatisticsQuery
        {
            StartDate = startDate,
            EndDate = endDate,
            GroupBy = groupBy
        };
        var result = await orderService.GetOrderStatistics(query);
        return Ok(result);
    }
}
