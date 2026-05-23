namespace PineSms.Core.Features.Order;

public class TrackOrderResult
{
    public bool Found { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string StatusTitle { get; set; } = string.Empty;
    public string? PostalTrackingCode { get; set; }
    public DateTime UpdatedAt { get; set; }
}
