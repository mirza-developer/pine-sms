using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PineSms.BaleBot.Models;
using PineSms.Persistence.Services;

namespace PineSms.BaleBot.Services;

/// <summary>
/// Handles incoming Bale bot updates.
/// - If the user sends an order code, replies with the current order status.
/// - Otherwise, sends a help/welcome message.
/// </summary>
public class BotUpdateHandler : IBotUpdateHandler
{
    private readonly BaleBotClient botClient;
    private readonly PineSmsDbContext dbContext;
    private readonly ILogger<BotUpdateHandler> logger;

    public BotUpdateHandler(BaleBotClient botClient, PineSmsDbContext dbContext, ILogger<BotUpdateHandler> logger)
    {
        this.botClient = botClient;
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task HandleAsync(BaleUpdate update, CancellationToken ct)
    {
        var message = update.Message;
        if (message == null || string.IsNullOrWhiteSpace(message.Text))
            return;

        var chatId = message.Chat.Id;
        var text = message.Text.Trim();

        logger.LogInformation("Update {UpdateId}: chat={ChatId} text={Text}", update.UpdateId, chatId, text);

        string replyText;

        // Check if the user is asking about an order by sending an order code
        var order = await dbContext.CustomerOrder
            .Include(o => o.OrderStatus)
            .FirstOrDefaultAsync(o => o.OrderCode == text, ct);

        if (order != null)
        {
            replyText = $"وضعیت سفارش «{order.OrderCode}»:\n{order.OrderStatus.Title}\n(آخرین به‌روزرسانی: {order.UpdatedAt:yyyy/MM/dd HH:mm})";
        }
        else if (text.StartsWith('/'))
        {
            replyText = text.ToLowerInvariant() switch
            {
                "/start" => "سلام! 👋\nبرای دریافت وضعیت سفارش خود، کد سفارش را ارسال کنید.",
                "/help"  => "راهنما:\n- کد سفارش را ارسال کنید تا وضعیت آن را دریافت کنید.",
                _        => "دستور ناشناخته. برای راهنمایی /help را ارسال کنید."
            };
        }
        else
        {
            replyText = "کد سفارش شما یافت نشد.\nلطفاً کد سفارش صحیح را وارد کنید یا برای راهنمایی /help را ارسال کنید.";
        }

        await botClient.SendMessageAsync(chatId, replyText, ct);
    }
}
