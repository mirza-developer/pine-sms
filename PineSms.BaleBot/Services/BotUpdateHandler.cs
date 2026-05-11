using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PineSms.BaleBot.Models;
using PineSms.BaleBot.Tools;
using PineSms.Persistence.Services;
using PineSms.Shared;

namespace PineSms.BaleBot.Services;

/// <summary>
/// Handles incoming Bale bot updates by acting as a transparent bypass between
/// the user and an AI chat agent powered by the Microsoft Agents SDK.
///
/// Flow:
///  1. <c>/start</c> — clears any existing session, greets the user via AI.
///  2. Any other message — forwarded to the AI agent using the user's persisted session.
///  3. If the AI response contains <c>&lt;&lt;ORDER_CODE … &gt;&gt;</c> blocks, the order
///     code is looked up in the database and the result is appended to the reply.
/// </summary>
public class BotUpdateHandler : IBotUpdateHandler
{
    private readonly BaleBotClient botClient;
    private readonly PineSmsDbContext dbContext;
    private readonly IChatAgentService agentService;
    private readonly ChatSessionStore sessionStore;
    private readonly ILogger<BotUpdateHandler> logger;

    public BotUpdateHandler(
        BaleBotClient botClient,
        PineSmsDbContext dbContext,
        IChatAgentService agentService,
        ChatSessionStore sessionStore,
        ILogger<BotUpdateHandler> logger)
    {
        this.botClient = botClient;
        this.dbContext = dbContext;
        this.agentService = agentService;
        this.sessionStore = sessionStore;
        this.logger = logger;
    }

    public async Task HandleAsync(BaleUpdate update, CancellationToken ct)
    {
        var message = update.Message;
        if (message == null || string.IsNullOrWhiteSpace(message.Text))
            return;

        var chatId = message.Chat.Id;
        var text = message.Text.Trim();

        logger.LogInformation("Update {UpdateId}: chat={ChatId}", update.UpdateId, chatId);

        // /start → reset session so user always gets a fresh greeting
        if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            sessionStore.RemoveSession(chatId);
        }

        // Forward to AI agent (creates a new session automatically when sessionJson is null)
        var existingSession = sessionStore.GetSession(chatId);
        var (rawResponse, updatedSession) = await agentService.SendWithSessionAsync(existingSession, text);
        sessionStore.SetSession(chatId, updatedSession);

        // Extract any ORDER_CODE blocks the AI embedded in its response
        var orderCodes = new List<string>();
        var visibleResponse = ResponseBlockTools.StripOrderCodeBlocks(rawResponse, orderCodes);

        // If the AI signalled one or more order codes, resolve them from the DB
        if (orderCodes.Count > 0)
        {
            var statusLines = new List<string>();
            foreach (var orderCode in orderCodes)
            {
                var order = await dbContext.CustomerOrder
                    .Include(o => o.OrderStatus)
                    .FirstOrDefaultAsync(o => o.OrderCode == orderCode, ct);

                if (order != null)
                {
                    statusLines.Add(
                        $"📦 سفارش «{order.OrderCode}»:\n" +
                        $"وضعیت: {order.OrderStatus.Title}\n" +
                        $"آخرین به‌روزرسانی: {PersianCalendarTools.GregorianToPersian(order.UpdatedAt)} {order.UpdatedAt:HH:mm}");
                }
                else
                {
                    statusLines.Add($"❌ سفارشی با کد «{orderCode}» یافت نشد.");
                }
            }

            var statusBlock = string.Join("\n\n", statusLines);

            // Append status info after the AI's visible text
            if (!string.IsNullOrWhiteSpace(visibleResponse))
                visibleResponse = visibleResponse + "\n\n" + statusBlock;
            else
                visibleResponse = statusBlock;
        }

        if (!string.IsNullOrWhiteSpace(visibleResponse))
            await botClient.SendMessageAsync(chatId, visibleResponse, ct);
    }
}

