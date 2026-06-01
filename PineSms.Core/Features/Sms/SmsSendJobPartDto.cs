namespace PineSms.Core.Features.Sms;

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
