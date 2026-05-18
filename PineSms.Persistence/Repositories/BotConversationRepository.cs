using Microsoft.EntityFrameworkCore;
using PineSms.Core.Contracts;
using PineSms.Core.Features.BotConversation;
using PineSms.Persistence.Services;

namespace PineSms.Persistence.Repositories;

public class BotConversationRepository : IBotConversationService
{
    private readonly PineSmsDbContext dbContext;

    public BotConversationRepository(PineSmsDbContext dbContext)
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
}
