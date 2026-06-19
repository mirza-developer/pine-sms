using Microsoft.AspNetCore.Mvc;
using PineAI.Core.Contracts;
using PineAI.Core.Features.Account;

namespace PineAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(IgnoreApi = true)]
public class AuthController : ControllerBase
{
    private readonly IAuthService authService;

    public AuthController(IAuthService authService)
    {
        this.authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] GetUserLoginQuery request)
    {
        var result = await authService.Authenticate(request);
        if (!result.Success)
            return Unauthorized(result);
        return Ok(result);
    }
}
