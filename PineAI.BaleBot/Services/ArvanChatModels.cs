using System.Text.Json;
using System.Text.Json.Serialization;

namespace PineAI.BaleBot.Services;

internal sealed record ArvanMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

internal sealed record ArvanChatRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] IReadOnlyList<ArvanMessage> Messages);

internal static class ArvanJsonOptions
{
    internal static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
