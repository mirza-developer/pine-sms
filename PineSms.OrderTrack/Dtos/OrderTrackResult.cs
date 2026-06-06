using System.Text.Json.Serialization;

namespace PineSms.OrderTrack;

public class OrderTrackResult
{
    public bool Found { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string StatusTitle { get; set; } = string.Empty;
    public string? PostalTrackingCode { get; set; }
    public DateTime UpdatedAt { get; set; }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(OrderTrackResult))]
public partial class OrderTrackResultContext : JsonSerializerContext
{

}
