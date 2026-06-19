namespace PineAI.Core.Features.Sms;

public class ScheduleSmsResult
{
    public bool Success { get; set; }
    public int JobId { get; set; }
    public string Message { get; set; } = string.Empty;
}
