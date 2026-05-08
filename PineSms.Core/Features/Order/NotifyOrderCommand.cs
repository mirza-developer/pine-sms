namespace PineSms.Core.Features.Order;

public class NotifyOrderCommand
{
    [Required]
    public string CustomerPhoneNumber { get; set; }

    [Required]
    public string OrderCode { get; set; } 

    [Required]
    public string OrderStatusCode { get; set; } 

    public string? PostTrackingCode { get; set; } 
}
