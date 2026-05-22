namespace PineSms.Core.Features.BotConversation;

public class BotChatMessageDto
{
    public int Id { get; set; }
    public string BaleUsername { get; set; } = string.Empty;
    public long ChatId { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public bool IsFromBot { get; set; }
    public DateTime SentAt { get; set; }
}
