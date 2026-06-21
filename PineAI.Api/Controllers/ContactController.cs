using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace PineAI.Api.Controllers;

public class ContactFormRequest
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string Description { get; set; } = string.Empty;
}

file class BaleSendRequest
{
    [JsonPropertyName("chat_id")]
    public string ChatId { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

[ApiController]
[Route("api/[controller]")]
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

    /// <summary>
    /// Receives a landing-page contact form submission and forwards it to the owner via Bale Bot.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] ContactFormRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("نام الزامی است");
        if (string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest("شماره تلفن الزامی است");
        if (string.IsNullOrWhiteSpace(request.Description))
            return BadRequest("توضیحات الزامی است");

        var token = configuration["BaleMessenger:ContactToken"];
        var chatId = configuration["BaleMessenger:ContactChatId"];

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chatId))
        {
            logger.LogWarning("BaleMessenger:ContactToken or BaleMessenger:ContactChatId not configured. Contact form submission not forwarded.");
            return Ok();
        }

        var text = BuildMessage(request);
        var baleUrl = $"https://tapi.bale.ai/bot{token}/sendMessage";

        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            var payload = new BaleSendRequest { ChatId = chatId, Text = text };
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
        sb.AppendLine($"💬 توضیحات:");
        sb.AppendLine(r.Description);
        return sb.ToString().TrimEnd();
    }
}
