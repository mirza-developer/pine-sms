namespace PineAI.Core.Features.Sms;

public class SmsSendJobDto
{
    public int Id { get; set; }
    public string FromNumber { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<SmsSendJobPartDto> Parts { get; set; } = new();
}
