using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PineAI.Core.Contracts;
using PineAI.Core.Features.Account;

namespace PineAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[ApiExplorerSettings(IgnoreApi = true)]
public class UserController : ControllerBase
{
    private readonly IAuthService authService;
    private readonly IMenuLinkService menuLinkService;

    public UserController(IAuthService authService, IMenuLinkService menuLinkService)
    {
        this.authService = authService;
        this.menuLinkService = menuLinkService;
    }

    /// <summary>Returns all non-admin users.</summary>
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll()
        => Ok(await authService.GetNonAdminUsersAsync());

    /// <summary>Creates a new non-admin user.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command)
    {
        var result = await authService.CreateUserAsync(command);
        if (!result.Success) return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    /// <summary>Updates a user's Persian name and optionally their password.</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserCommand command)
    {
        command.Id = id;
        var result = await authService.UpdateUserAsync(command);
        if (!result.Success) return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    /// <summary>Deletes a non-admin user and clears their menu link assignments.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        // Clear the user's menu link assignments before deleting the identity record
        await menuLinkService.SaveUserMenuLinksAsync(id, []);

        var result = await authService.DeleteUserAsync(id);
        if (!result.Success) return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }
}
