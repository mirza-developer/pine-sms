namespace PineSms.Core.Features.Order;

public class NotifyOrderCommand
{
    [Required]
    public string CustomerPhoneNumber { get; set; } = string.Empty;

    [Required]
    public string OrderCode { get; set; } = string.Empty;

    [Required]
    public string OrderStatusCode { get; set; } = string.Empty;
}
