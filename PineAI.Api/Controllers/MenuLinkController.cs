using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PineAI.Core.Contracts;
using PineAI.Core.Features.MenuLink;
using System.Security.Claims;

namespace PineAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MenuLinkController : ControllerBase
{
    private readonly IMenuLinkService menuLinkService;
    private readonly IAuthService authService;

    public MenuLinkController(IMenuLinkService menuLinkService, IAuthService authService)
    {
        this.menuLinkService = menuLinkService;
        this.authService = authService;
    }

    /// <summary>
    /// Returns accessible links for the currently logged-in user.
    /// Admin users receive all links; others receive only their assigned links.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<MenuLinkDto>>> GetMyLinks()
    {
        bool isAdmin = User.IsInRole("Admin");
        if (isAdmin)
            return Ok(await menuLinkService.GetAllMenuLinksAsync());

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        return Ok(await menuLinkService.GetUserMenuLinksAsync(userId));
    }

    /// <summary>Returns all menu links (admin only).</summary>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<MenuLinkDto>>> GetAll()
    {
        return Ok(await menuLinkService.GetAllMenuLinksAsync());
    }

    /// <summary>Returns all users (admin only).</summary>
    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<Core.Features.Account.UserDto>>> GetUsers()
    {
        return Ok(await authService.GetAllUsersAsync());
    }

    /// <summary>Returns the menu link IDs assigned to a specific user (admin only).</summary>
    [HttpGet("users/{userId}/links")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<int>>> GetUserLinks(string userId)
    {
        var links = await menuLinkService.GetUserMenuLinksAsync(userId);
        return Ok(links.Select(l => l.Id).ToList());
    }

    /// <summary>Saves the menu links assigned to a specific user (admin only).</summary>
    [HttpPost("users/{userId}/links")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SaveUserLinks(string userId, [FromBody] SaveUserMenuLinksCommand command)
    {
        await menuLinkService.SaveUserMenuLinksAsync(userId, command.MenuLinkIds);
        return Ok();
    }
}
