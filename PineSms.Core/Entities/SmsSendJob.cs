namespace PineSms.Core.Entities;

public class SmsSendJob : IBaseEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string FromNumber { get; set; } = string.Empty;

    [Required]
    public string MessageText { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; }

    public ICollection<SmsSendJobPart> Parts { get; set; } = new List<SmsSendJobPart>();
}
