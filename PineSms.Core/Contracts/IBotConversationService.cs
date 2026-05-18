using PineSms.Core.Features.BotConversation;

namespace PineSms.Core.Contracts;

public interface IBotConversationService
{
    /// <summary>Returns all chat messages for the given Bale username, ordered by SentAt ascending.</summary>
    Task<List<BotChatMessageDto>> GetConversationAsync(string baleUsername);
}
