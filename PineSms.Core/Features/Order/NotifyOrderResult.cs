namespace PineSms.Core.Features.Order;

public class NotifyOrderResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsNewCustomer { get; set; }
    public bool IsNewOrder { get; set; }
    public bool NotificationSent { get; set; }
}
