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

    /// <summary>
    /// Returns a paginated list of unique Bale usernames with latest message date and message count.
    /// </summary>
    [HttpGet("user-summaries")]
    public async Task<IActionResult> GetUserSummaries([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (pageSize is < 1 or > 100) pageSize = 10;
        var result = await botConversationService.GetUserSummariesPagedAsync(page, pageSize);
        return Ok(result);
    }
}
