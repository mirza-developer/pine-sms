using Microsoft.EntityFrameworkCore;
using PineAI.Core.Contracts;
using PineAI.Core.Features.BotConversation;
using PineAI.Persistence.Services;

namespace PineAI.Persistence.Repositories;

public class BotConversationRepository : IBotConversationService
{
    private readonly PineAIDbContext dbContext;

    public BotConversationRepository(PineAIDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<List<BotChatMessageDto>> GetConversationAsync(string baleUsername)
    {
        return await dbContext.BotChatMessage
            .Where(m => m.BaleUsername == baleUsername)
            .OrderBy(m => m.SentAt)
            .Select(m => new BotChatMessageDto
            {
                Id = m.Id,
                BaleUsername = m.BaleUsername,
                ChatId = m.ChatId,
                MessageText = m.MessageText,
                IsFromBot = m.IsFromBot,
                SentAt = m.SentAt
            })
            .ToListAsync();
    }

    public async Task<BotUserSummaryPageResult> GetUserSummariesPagedAsync(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        // Group by username, take the latest ChatId per username (they share one ChatId anyway)
        var query = dbContext.BotChatMessage
            .GroupBy(m => m.BaleUsername)
            .Select(g => new BotUserSummaryDto
            {
                BaleUsername = g.Key,
                ChatId = g.First().ChatId,
                LastMessageAt = g.Max(m => m.SentAt),
                MessageCount = g.Count()
            })
            .OrderByDescending(s => s.LastMessageAt);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new BotUserSummaryPageResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
