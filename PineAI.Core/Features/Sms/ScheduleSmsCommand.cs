namespace PineAI.Core.Features.Sms;

public class ScheduleSmsCommand
{
    public List<int> CustomerIds { get; set; } = new();
    public string MessageText { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
    /// <summary>Number of equal parts to divide recipients into.</summary>
    public int NumberOfParts { get; set; } = 1;
    /// <summary>Delay in minutes between consecutive parts.</summary>
    public int DelayMinutesBetweenParts { get; set; } = 0;
    /// <summary>UTC time to send the first part. Null means send the first part immediately.</summary>
    public DateTime? FirstSendAt { get; set; }
}
