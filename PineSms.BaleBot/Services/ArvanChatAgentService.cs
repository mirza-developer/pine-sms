using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PineSms.BaleBot.Services;

/// <summary>
/// A session-aware AI chat agent that uses the ArvanCloud AIaaS API
/// (OpenAI-compatible REST endpoint) as its backend.
/// Session state is maintained as a plain JSON array of chat messages so it can
/// be persisted and restored across requests without any third-party SDK.
/// </summary>
public class ArvanChatAgentService : IChatAgentService
{
    private readonly IConfiguration configuration;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<ArvanChatAgentService> logger;

    private HttpClient? httpClient;
    private string model = "openai/gpt-4o-mini";
    private string systemInstructions = string.Empty;

    public ArvanChatAgentService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<ArvanChatAgentService> logger)
    {
        this.configuration = configuration;
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task InitAsync()
    {
        var apiKey = configuration["ArvanAiAgent:ApiKey"] ?? string.Empty;
        model = configuration["ArvanAiAgent:Model"] ?? "openai/gpt-4o-mini";
        var endpoint = configuration["ArvanAiAgent:Endpoint"] ?? "https://text.arvancloud.ir/oai/v1";

        // Normalize the base address so relative paths resolve correctly.
        var baseAddress = endpoint.TrimEnd('/') + "/";

        httpClient = httpClientFactory.CreateClient("ArvanAiClient");
        httpClient.BaseAddress = new Uri(baseAddress);
        httpClient.Timeout = TimeSpan.FromSeconds(60);
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("apikey", apiKey);

        systemInstructions = await LoadInstructionsAsync();

        logger.LogInformation(
            "ArvanChatAgentService initialized (model={Model}, endpoint={Endpoint})",
            model, endpoint);
    }

    /// <inheritdoc/>
    public async Task<(string ResponseText, string SerializedSession)> SendWithSessionAsync(
        string? sessionJson, string userText)
    {
        EnsureInitialized();

        // ── Reconstruct conversation history ─────────────────────────────────
        List<ArvanMessage> history;
        if (!string.IsNullOrWhiteSpace(sessionJson))
        {
            history = JsonSerializer.Deserialize<List<ArvanMessage>>(sessionJson,
                          JsonOptions.Default)
                      ?? new List<ArvanMessage>();
        }
        else
        {
            history = new List<ArvanMessage>();
        }

        // Build the messages array sent to the API (system prompt + history + new turn)
        var messages = new List<ArvanMessage>();
        if (!string.IsNullOrWhiteSpace(systemInstructions))
            messages.Add(new ArvanMessage("system", systemInstructions));
        messages.AddRange(history);
        messages.Add(new ArvanMessage("user", userText));

        // ── Call ArvanCloud Chat Completions API ──────────────────────────────
        var requestBody = new ArvanChatRequest(model, messages);
        var response = await httpClient!.PostAsJsonAsync("chat/completions", requestBody, JsonOptions.Default);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadFromJsonAsync<JsonElement>();
        var responseText = responseBody
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        // ── Persist the updated history (without the system prompt) ───────────
        history.Add(new ArvanMessage("user", userText));
        history.Add(new ArvanMessage("assistant", responseText));

        var serializedSession = JsonSerializer.Serialize(history, JsonOptions.Default);

        return (responseText, serializedSession);
    }

    /// <inheritdoc/>
    public Task<string> CreateNewSessionJsonAsync()
    {
        EnsureInitialized();
        // An empty history array is a valid "new session".
        return Task.FromResult("[]");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void EnsureInitialized()
    {
        if (httpClient == null)
            throw new InvalidOperationException(
                "ArvanChatAgentService has not been initialized. Call InitAsync() first.");
    }

    /// <summary>
    /// Loads all instruction Markdown files from the <c>Chat/</c> folder next to the
    /// running assembly and concatenates them into a single system-prompt string.
    /// </summary>
    private static async Task<string> LoadInstructionsAsync()
    {
        var chatDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Chat");
        if (!Directory.Exists(chatDir))
            return string.Empty;

        var lines = new List<string>();
        foreach (var filePath in Directory.GetFiles(chatDir, "*.md", SearchOption.TopDirectoryOnly))
        {
            var content = await File.ReadAllTextAsync(filePath);
            lines.Add($"=== {Path.GetFileName(filePath)} ===");
            lines.Add(content);
            lines.Add(string.Empty);
        }

        return string.Join(Environment.NewLine, lines);
    }

    // ── Private model types ───────────────────────────────────────────────────

    private sealed record ArvanMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ArvanChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ArvanMessage> Messages);

    private static class JsonOptions
    {
        internal static readonly JsonSerializerOptions Default = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }
}
