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

    [HttpPost("schedule")]
    public async Task<IActionResult> ScheduleSms([FromBody] ScheduleSmsCommand command)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var result = await smsService.ScheduleSms(command, userId);
        return Ok(result);
    }

    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobs()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var result = await smsService.GetSmsJobs(userId);
        return Ok(result);
    }

    [HttpGet("jobs/{id:int}")]
    public async Task<IActionResult> GetJob(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var result = await smsService.GetSmsJob(id, userId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Queries the Melipayamak provider for the current delivery status of the given recIds.
    /// Pass the recIds stored in SmsLog.RecipientsJson or SmsSendJobPart.ResultJson.
    /// </summary>
    [HttpPost("delivery-status")]
    public async Task<IActionResult> GetDeliveryStatus([FromBody] long[] recIds)
    {
        if (recIds == null || recIds.Length == 0)
            return BadRequest("حداقل یک recId الزامی است");

        var result = await smsService.GetSmsDeliveryStatus(recIds);
        return Ok(result);
    }
}
