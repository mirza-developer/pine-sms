namespace PineAI.Core.Features.Order;

public class OrderStatisticsDataPoint
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime Date { get; set; }
}
