namespace PineSms.Core.Features.BotConversation;

/// <summary>Summary row shown in the paginated user grid on the admin conversations page.</summary>
public class BotUserSummaryDto
{
    public string BaleUsername { get; set; } = string.Empty;
    public long ChatId { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int MessageCount { get; set; }
}
