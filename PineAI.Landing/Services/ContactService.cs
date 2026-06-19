using System.Net.Http.Json;

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

    public ContactService(HttpClient httpClient, SiteSettings settings)
    {
        this.httpClient = httpClient;
        this.settings = settings;
    }

    public async Task<bool> SendAsync(ContactFormDto form, CancellationToken ct = default)
    {
        var url = settings.ContactApiUrl;
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            var response = await httpClient.PostAsJsonAsync(url, form, ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
