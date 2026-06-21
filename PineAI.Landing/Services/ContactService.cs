using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace PineAI.Landing.Services;

public class ContactFormDto
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ContactService
{
    private readonly HttpClient httpClient;
    private readonly SiteSettings settings;

    private readonly ILogger<ContactService> logger;

    public ContactService(HttpClient httpClient
        , SiteSettings settings
        , ILogger<ContactService> logger)
    {
        this.httpClient = httpClient;
        this.settings = settings;
        this.logger = logger;
    }

    public async Task<bool> SendMessageAsync(ContactFormDto data, CancellationToken ct)
    {

        try
        {
            var response = await httpClient.PostAsJsonAsync("api/contact", data, ct);
            if (response.IsSuccessStatusCode)
                return true;

            var error = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("sendMessage failed: {StatusCode} {Error}", response.StatusCode, error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception sending message to chat {ChatId}", 451596244);
        }
        return false;
    }
}
