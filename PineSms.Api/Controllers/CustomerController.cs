using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PineSms.Core.Contracts;
using PineSms.Core.Features.Customer;
using System.Security.Claims;

namespace PineSms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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
        var (success, message) = await customerService.InsertCustomer(command, userId);
        if (!success) return BadRequest(new { message });
        return Ok(new { message });
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
    public async Task<IActionResult> GetByRange([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var customers = await customerService.GetCustomersByDateRange(from, to);
        return Ok(customers);
    }

    [HttpGet("byphone/{phone}")]
    public async Task<IActionResult> GetByPhone(string phone)
    {
        var customer = await customerService.GetCustomerByPhoneNumber(phone);
        if (customer == null) return NotFound(new { message = "مشتری با این شماره یافت نشد" });
        return Ok(customer);
    }
}
