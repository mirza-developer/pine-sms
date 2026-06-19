namespace PineAI.Core.Features.Order;

public class DateRangeResult
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string GroupBy { get; set; } = string.Empty;
}
