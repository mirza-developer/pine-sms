namespace PineSms.Core.Features.Sms;

public class SendSmsResult
{
    public bool Success { get; set; }
    public int SentCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
