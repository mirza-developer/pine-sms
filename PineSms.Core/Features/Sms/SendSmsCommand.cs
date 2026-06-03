namespace PineSms.Core.Features.Sms;

public class SendSmsCommand
{
    public List<int> CustomerIds { get; set; } = new();
    public string MessageText { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
    public string DateRangeType { get; set; } = string.Empty;
    public DateTime? CustomFrom { get; set; }
    public DateTime? CustomTo { get; set; }
}
