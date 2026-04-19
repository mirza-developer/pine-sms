using Microsoft.AspNetCore.Mvc;
using PineSms.Core.Contracts;
using PineSms.Core.Features.Account;

namespace PineSms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
