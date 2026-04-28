using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PineSms.Core.Contracts;

namespace PineSms.Persistence.Services.Messaging;

/// <summary>
/// Sends notifications via Bale Safir API (https://docs.bale.ai/safir).
/// Configure BaleMessenger:Token and BaleMessenger:BaseUrl in appsettings.
/// </summary>
public class BaleMessengerService : IBaleMessengerService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<BaleMessengerService> logger;
    private readonly string token;

    public BaleMessengerService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<BaleMessengerService> logger)
    {
        this.httpClient = httpClientFactory.CreateClient("BaleMessenger");
        this.logger = logger;
        this.token = configuration["BaleMessenger:Token"] ?? string.Empty;
    }

    public async Task<bool> SendMessageAsync(string phoneNumber, string message)
    {
        if (string.IsNullOrEmpty(token))
        {
            logger.LogWarning("BaleMessenger token is not configured. Skipping notification.");
            return false;
        }

        // Sanitize user-provided input before writing to logs to prevent log-forging
        var safePhone = SanitizeForLog(phoneNumber);

        try
        {
            // Bale Safir API: POST /bot{token}/sendMessage
            var requestBody = new
            {
                phone = phoneNumber,
                text = message
            };

            var response = await httpClient.PostAsJsonAsync($"bot{token}/sendMessage", requestBody);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Bale message sent to {PhoneNumber}", safePhone);
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            logger.LogWarning("Bale message failed for {PhoneNumber}: {StatusCode} {Error}",
                safePhone, response.StatusCode, SanitizeForLog(errorContent));
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception sending Bale message to {PhoneNumber}", safePhone);
            return false;
        }
    }

    private static string SanitizeForLog(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        // Remove newlines and carriage returns that could enable log injection
        return value.Replace("\r", "\\r").Replace("\n", "\\n");
    }
}
