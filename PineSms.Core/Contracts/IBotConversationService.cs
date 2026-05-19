using PineSms.Core.Features.BotConversation;

namespace PineSms.Core.Contracts;

public interface IBotConversationService
{
    /// <summary>Returns all chat messages for the given Bale username, ordered by SentAt ascending.</summary>
    Task<List<BotChatMessageDto>> GetConversationAsync(string baleUsername);

    /// <summary>
    /// Returns a paged list of unique Bale usernames, each with their latest message datetime
    /// and total message count, ordered by most-recent message first.
    /// </summary>
    Task<BotUserSummaryPageResult> GetUserSummariesPagedAsync(int page, int pageSize);
}
