using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PineSms.Core.Contracts;

namespace PineSms.Persistence.Services.Messaging;

/// <summary>
/// Sends notifications via Bale Safir API (https://docs.bale.ai/safir).
/// Configure BaleMessenger:ApiAccessKey and BaleMessenger:BotId in appsettings.
/// </summary>
public class BaleMessengerService : IBaleMessengerService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<BaleMessengerService> logger;
    private readonly string apiAccessKey;
    private readonly long botId;

    public BaleMessengerService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<BaleMessengerService> logger)
    {
        this.httpClient = httpClientFactory.CreateClient("BaleMessenger");
        this.logger = logger;
        this.apiAccessKey = configuration["BaleMessenger:Safir"] ?? string.Empty;
        this.botId = long.TryParse(configuration["BaleMessenger:BotId"], out var id) ? id : 0;
    }

    public async Task<bool> SendMessageAsync(string phoneNumber, string message)
    {
        if (string.IsNullOrEmpty(apiAccessKey))
        {
            logger.LogWarning("BaleMessenger API access key is not configured. Skipping notification.");
            return false;
        }

        // Sanitize user-provided input before writing to logs to prevent log-forging
        var safePhone = SanitizeForLog(phoneNumber);

        try
        {
            // Bale Safir API v3: POST /api/v3/send_message
            var requestBody = new
            {
                bot_id = botId,
                phone_number = $"98{phoneNumber}",
                message_data = new
                {
                    message = new
                    {
                        text = message
                    }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v3/send_message");
            request.Headers.Add("api-access-key", apiAccessKey);
            request.Content = JsonContent.Create(requestBody);

            var response = await httpClient.SendAsync(request);

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
