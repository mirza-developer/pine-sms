namespace PineSms.Core.Features.Order;

public class OrderStatisticsResult
{
    public List<OrderStatisticsDataPoint> DataPoints { get; set; } = new();
}

public class OrderStatisticsDataPoint
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime Date { get; set; }
}
