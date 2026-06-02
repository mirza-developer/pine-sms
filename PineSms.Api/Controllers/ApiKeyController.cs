using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PineSms.Core.Contracts;
using PineSms.Core.Features.ApiKey;

namespace PineSms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class ApiKeyController : ControllerBase
{
    private readonly IApiKeyService apiKeyService;

    public ApiKeyController(IApiKeyService apiKeyService)
    {
        this.apiKeyService = apiKeyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var keys = await apiKeyService.GetAllApiKeys();
        return Ok(keys);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApiKeyCommand command)
    {
        var result = await apiKeyService.CreateApiKey(command);
        if (!result.Success)
            return BadRequest(new { result.Message });
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await apiKeyService.DeleteApiKey(id);
        if (!result.Success) return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }
}
