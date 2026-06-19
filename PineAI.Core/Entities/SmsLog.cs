namespace PineAI.Core.Entities;

public class SmsLog : IBaseEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public DateTime SendDate { get; set; }

    [Required]
    public string SendUserId { get; set; } = string.Empty;

    [Required]
    public string MessageText { get; set; } = string.Empty;

    [Required]
    public string FromNumber { get; set; } = string.Empty;

    // JSON array of {PhoneNumber, Result}
    [Required]
    public string RecipientsJson { get; set; } = "[]";
}
