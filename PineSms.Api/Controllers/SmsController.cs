using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PineSms.Core.Contracts;
using PineSms.Core.Features.Sms;
using System.Security.Claims;

namespace PineSms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SmsController : ControllerBase
{
    private readonly ISmsService smsService;

    public SmsController(ISmsService smsService)
    {
        this.smsService = smsService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendSms([FromBody] SendSmsCommand command)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var result = await smsService.SendSms(command, userId);
        return Ok(result);
    }
}
