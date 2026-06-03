namespace PineSms.Core.Features.Order;

public class BulkUpdateTrackingResult
{
    public bool Success { get; set; }
    public int UpdatedCount { get; set; }
    public int NotFoundCount { get; set; }
    public List<string> NotFoundCodes { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
