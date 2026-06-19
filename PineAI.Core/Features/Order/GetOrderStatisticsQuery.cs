namespace PineAI.Core.Features.Order;

public class GetOrderStatisticsQuery
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string GroupBy { get; set; } = "day"; // "day", "week", "month", "year"
}
