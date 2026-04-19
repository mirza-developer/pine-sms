namespace PineSms.Core.Features.Sms;

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

public class ScheduleSmsResult
{
    public bool Success { get; set; }
    public int JobId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class SmsSendJobDto
{
    public int Id { get; set; }
    public string FromNumber { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<SmsSendJobPartDto> Parts { get; set; } = new();
}

public class SmsSendJobPartDto
{
    public int Id { get; set; }
    public int PartNumber { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int RecipientCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int SentCount { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public string? ResultJson { get; set; }
}
