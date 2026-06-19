namespace PineAI.Core.Entities;

/// <summary>Status values for a SmsSendJobPart.</summary>
public enum SmsJobPartStatus
{
    Pending = 0,
    Sending = 1,
    Completed = 2,
    Failed = 3
}

public class SmsSendJobPart : IBaseEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int JobId { get; set; }

    public SmsSendJob Job { get; set; } = null!;

    /// <summary>1-based index of this part within its job.</summary>
    public int PartNumber { get; set; }

    /// <summary>UTC time at which this part should be sent.</summary>
    [Required]
    public DateTime ScheduledAt { get; set; }

    /// <summary>JSON array of Customer IDs to send to in this part.</summary>
    [Required]
    public string CustomerIdsJson { get; set; } = "[]";

    public SmsJobPartStatus Status { get; set; } = SmsJobPartStatus.Pending;

    public int SentCount { get; set; }

    public DateTime? ExecutedAt { get; set; }

    public string? ResultJson { get; set; }
}
