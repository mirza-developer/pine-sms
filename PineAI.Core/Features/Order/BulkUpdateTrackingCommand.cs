namespace PineAI.Core.Features.Order;

public class BulkUpdateTrackingCommand
{
    public List<TrackingEntry> Entries { get; set; } = new();
}
