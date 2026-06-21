using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PineAI.Api.Landing.Auth;

namespace PineAI.Api.Landing.Controllers;

public class ContactFormRequest
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class BaleSendRequest
{
    [JsonPropertyName("chat_id")]
    public string ChatId { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
public class ContactController : ControllerBase
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IConfiguration configuration;
    private readonly ILogger<ContactController> logger;

    public ContactController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ContactController> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.configuration = configuration;
        this.logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] ContactFormRequest request, CancellationToken ct)
    {
        var token = configuration["BaleMessenger:ContactToken"];
        var chatId = configuration["BaleMessenger:ContactChatId"];
        var baleUrl = $"https://tapi.bale.ai/bot{token}/sendMessage";

        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            var payload = new BaleSendRequest { ChatId = chatId, Text = BuildMessage(request) };
            var response = await client.PostAsJsonAsync(baleUrl, payload, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Bale sendMessage failed: {StatusCode} {Error}", response.StatusCode, error);
                return StatusCode(500, "خطا در ارسال پیام");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception forwarding contact form to Bale");
            return StatusCode(500, "خطا در ارسال پیام");
        }

        return Ok();
    }

    private static string BuildMessage(ContactFormRequest r)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("📋 درخواست جدید از سایت پاین‌ای");
        sb.AppendLine("─────────────────────");
        sb.AppendLine($"👤 نام: {r.Name}");
        sb.AppendLine($"📞 تلفن: {r.Phone}");
        if (!string.IsNullOrWhiteSpace(r.Website))
            sb.AppendLine($"🌐 وب‌سایت: {r.Website}");
        sb.AppendLine("💬 توضیحات:");
        sb.AppendLine(r.Description);
        return sb.ToString().TrimEnd();
    }
}
