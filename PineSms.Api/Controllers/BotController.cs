using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PineSms.Core.Contracts;

namespace PineSms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class BotController : ControllerBase
{
    private readonly IBotConversationService botConversationService;

    public BotController(IBotConversationService botConversationService)
    {
        this.botConversationService = botConversationService;
    }

    /// <summary>
    /// Returns all bot conversation messages for the given Bale username.
    /// </summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversation([FromQuery] string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest("نام کاربری بله الزامی است");

        var messages = await botConversationService.GetConversationAsync(username.Trim());
        return Ok(messages);
    }
}
