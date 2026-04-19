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

public class SendSmsResult
{
    public bool Success { get; set; }
    public int SentCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
