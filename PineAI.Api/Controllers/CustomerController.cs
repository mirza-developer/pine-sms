using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PineAI.Core.Contracts;
using PineAI.Core.Features.Customer;
using System.Security.Claims;

namespace PineAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService customerService;

    public CustomerController(ICustomerService customerService)
    {
        this.customerService = customerService;
    }

    [HttpPost]
    public async Task<IActionResult> InsertCustomer([FromBody] InsertCustomerCommand command)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var result = await customerService.InsertCustomer(command, userId);
        if (!result.Success) return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportCustomers([FromBody] ImportCustomersCommand command)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var result = await customerService.ImportCustomers(command, userId);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("byrange")]
    public async Task<IActionResult> GetByRange([FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] string? phonePrefix = null, [FromQuery] bool? isTester = null)
    {
        var customers = await customerService.GetCustomersByDateRange(from, to, phonePrefix, isTester);
        return Ok(customers);
    }

    [HttpGet("byphone/{phone}")]
    public async Task<IActionResult> GetByPhone(string phone)
    {
        var result = await customerService.GetCustomerByPhoneNumber(phone);
        if (result.Customer == null) return NotFound(new { message = result.ErrorMessage ?? "مشتری با این شماره یافت نشد" });
        return Ok(result.Customer);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerCommand command)
    {
        if (command.Id != id)
            return BadRequest(new { message = "شناسه مشتری مطابقت ندارد" });

        var result = await customerService.UpdateCustomer(command);
        if (!result.Success) return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }
}
