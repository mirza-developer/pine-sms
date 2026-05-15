using System.Text.Json;
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
///  4. If the AI response contains <c>&lt;&lt;FEEDBACK … &gt;&gt;</c> blocks, the feedback
///     is routed to the appropriate chat ID based on the feedback type (10 different types supported).
/// </summary>
public class BotUpdateHandler : IBotUpdateHandler
{
    private readonly BaleBotClient botClient;
    private readonly PineSmsDbContext dbContext;
    private readonly IChatAgentService agentService;
    private readonly ChatSessionStore sessionStore;
    private readonly ILogger<BotUpdateHandler> logger;

    // Feedback routing table: dynamically loaded from markdown instructions file
    private static readonly Lazy<Dictionary<string, long>> FeedbackRoutingTable = new(() =>
    {
        try
        {
            var markdownPath = FeedbackRoutingTableParser.GetMarkdownFilePath();
            return FeedbackRoutingTableParser.ParseRoutingTable(markdownPath);
        }
        catch (Exception ex)
        {
            // Fallback to empty dictionary if parsing fails
            Console.WriteLine($"Warning: Failed to load feedback routing table from markdown: {ex.Message}");
            return new Dictionary<string, long>();
        }
    });

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
        if (message is null || string.IsNullOrWhiteSpace(message.Text))
            return;

        var chatId = message.Chat.Id;
        var text = message.Text.Trim();

        logger.LogInformation("Update {UpdateId}: chat={ChatId}", update.UpdateId, chatId);

        // /start → reset session so user always gets a fresh greeting
        if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            sessionStore.RemoveSession(chatId);
        }

        var existingSession = sessionStore.GetSession(chatId);
        var (rawResponse, updatedSession) = await agentService.SendWithSessionAsync(existingSession, text);
        sessionStore.SetSession(chatId, updatedSession);

        var orderCodes = new List<string>();
        var visibleOrderCodes = ResponseBlockTools.StripOrderCodeBlocks(rawResponse, orderCodes);
        visibleOrderCodes = ResponseBlockTools.StripFeedbackBlocks(visibleOrderCodes, out var feedbackJson);

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
            if (!string.IsNullOrWhiteSpace(visibleOrderCodes))
                visibleOrderCodes = visibleOrderCodes + "\n\n" + statusBlock;
            else
                visibleOrderCodes = statusBlock;

            if (!string.IsNullOrWhiteSpace(visibleOrderCodes))
                await botClient.SendMessageAsync(chatId, visibleOrderCodes, ct);
        }
        else if (!string.IsNullOrEmpty(feedbackJson))
        {
            await HandleFeedbackAsync(chatId, feedbackJson, update.Message.From.Username, ct);
        }
        else
        {
            await botClient.SendMessageAsync(chatId, visibleOrderCodes, ct);
        }
    }

    private async Task HandleFeedbackAsync(long userChatId, string feedbackJson, string username, CancellationToken ct)
    {
        using var feedbackDoc = JsonDocument.Parse(feedbackJson);
        var root = feedbackDoc.RootElement;

        // Extract feedback type to determine routing
        if (!root.TryGetProperty("Type", out var typeProperty))
        {
            logger.LogWarning("Feedback JSON missing 'Type' field");
            return;
        }

        string feedbackType = typeProperty.GetString() ?? string.Empty;

        // Look up target chat ID from routing table
        if (!FeedbackRoutingTable.Value.TryGetValue(feedbackType, out long targetChatId))
        {
            logger.LogWarning("Unknown feedback type: {FeedbackType}", feedbackType);
            return;
        }

        // Skip routing if chat ID is not configured (0 means placeholder)
        if (targetChatId == 0)
        {
            logger.LogWarning("Chat ID not configured for feedback type: {FeedbackType}", feedbackType);
            await botClient.SendMessageAsync(userChatId, 
                "✅ اطلاعات شما ثبت شد. به زودی پشتیبانی با شما تماس خواهد گرفت.", ct);
            return;
        }

        string userBaleUsername = $"\n کاربری: @{username}";

        // Route to appropriate handler based on feedback type
        switch (feedbackType)
        {
            case "Satisfaction":
                await HandleSatisfactionAsync(userChatId, targetChatId, root, userBaleUsername, ct);
                break;

            case "Complaint":
                await HandleComplaintAsync(userChatId, targetChatId, root, userBaleUsername, ct);
                break;

            case "DefectiveProduct":
                await HandleDefectiveProductAsync(userChatId, targetChatId, root, userBaleUsername, ct);
                break;

            case "PhotoMismatch":
                await HandlePhotoMismatchAsync(userChatId, targetChatId, root, userBaleUsername, ct);
                break;

            case "ReturnedPackage":
                await HandleReturnedPackageAsync(userChatId, targetChatId, root, userBaleUsername, ct);
                break;

            case "Wholesale":
                await HandleWholesaleAsync(userChatId, targetChatId, root, userBaleUsername, ct);
                break;

            case "NoOrderCode":
                await HandleNoOrderCodeAsync(userChatId, targetChatId, root, userBaleUsername, ct);
                break;

            case "FailedPayment":
                await HandleFailedPaymentAsync(userChatId, targetChatId, root, userBaleUsername, ct);
                break;

            case "DelayedDelivery":
                await HandleDelayedDeliveryAsync(userChatId, targetChatId, root, userBaleUsername, ct);
                break;

            case "WrongSize":
                await HandleWrongSizeAsync(userChatId, targetChatId, root, userBaleUsername, ct);
                break;

            default:
                logger.LogWarning("Unhandled feedback type: {FeedbackType}", feedbackType);
                break;
        }
    }

    private async Task HandleSatisfactionAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, CancellationToken ct)
    {
        string messageSatisfactionSuccess = "🌸 پیام شما با موفقیت ثبت شد و به مدیریت ارسال گردید. از همراهی شما سپاسگزاریم.";
        await botClient.SendMessageAsync(userChatId, messageSatisfactionSuccess, ct);

        string orderCode = root.TryGetProperty("OrderCode", out var ocProp) ? ocProp.GetString() : "نامشخص";
        string description = root.TryGetProperty("Description", out var descProp) ? descProp.GetString() : "";

        string satisfactionLog = $"🌸 پیام رضایت جدید ثبت شد:\n" +
            $"کد سفارش: {orderCode}\n" +
            $"توضیحات: {description}" +
            userBaleUsername;

        await botClient.SendMessageAsync(targetChatId, satisfactionLog, ct);
    }

    private async Task HandleComplaintAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, CancellationToken ct)
    {
        string messageComplaintSuccess = "📣 اطلاعات شما ثبت شد:\nپشتیبانی ما در اسرع وقت با شما تماس خواهد گرفت.";
        await botClient.SendMessageAsync(userChatId, messageComplaintSuccess, ct);

        var orderCode = root.GetProperty("OrderCode").GetString();
        var phoneNumber = root.GetProperty("PhoneNumber").GetString();
        var date = root.GetProperty("Date").GetString();
        var description = root.GetProperty("Description").GetString();

        string complaintLog = $"📣 شکایت/درخواست پیگیری جدید ثبت شد:\n" +
            $"کد سفارش: {orderCode}\n" +
            $"شماره تماس: {phoneNumber}\n" +
            $"تاریخ: {date}\n" +
            $"توضیحات: {description}\n";

        var order = await dbContext.CustomerOrder
               .Include(o => o.OrderStatus)
               .FirstOrDefaultAsync(o => o.OrderCode == orderCode, ct);

        if (order is not null)
        {
            complaintLog += "\n" +
                $"📦 سفارش «{order.OrderCode}»:\n" +
                $"وضعیت: {order.OrderStatus.Title}\n" +
                $"آخرین به‌روزرسانی: {PersianCalendarTools.GregorianToPersian(order.UpdatedAt)} {order.UpdatedAt:HH:mm}";
        }
        else
        {
            complaintLog += "\n" + $"❌ سفارشی با کد «{orderCode}» یافت نشد.";
        }

        complaintLog += userBaleUsername + "\n #case ";

        await botClient.SendMessageAsync(targetChatId, complaintLog, ct);
    }

    private async Task HandleDefectiveProductAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, CancellationToken ct)
    {
        string messageSuccess = "✅ اطلاعات شما ثبت شد. پشتیبانی ما در اسرع وقت با شما تماس خواهد گرفت.";
        await botClient.SendMessageAsync(userChatId, messageSuccess, ct);

        var orderCode = root.GetProperty("OrderCode").GetString();
        var phoneNumber = root.GetProperty("PhoneNumber").GetString();
        var description = root.GetProperty("Description").GetString();
        bool hasPhoto = root.TryGetProperty("HasPhoto", out var photoEl) && photoEl.GetBoolean();

        string defectiveLog = $"⚠️ گزارش محصول معیوب/خراب:\n" +
            $"کد سفارش: {orderCode}\n" +
            $"شماره تماس: {phoneNumber}\n" +
            $"توضیحات: {description}\n" +
            $"عکس ارسال شده: {(hasPhoto ? "بله" : "خیر")}\n";

        var order = await LookupOrderAsync(orderCode, ct);
        defectiveLog += order + userBaleUsername + "\n #defective";

        await botClient.SendMessageAsync(targetChatId, defectiveLog, ct);
    }

    private async Task HandlePhotoMismatchAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, CancellationToken ct)
    {
        string messageSuccess = "✅ اطلاعات شما ثبت شد. پشتیبانی ما در اسرع وقت با شما تماس خواهد گرفت.";
        await botClient.SendMessageAsync(userChatId, messageSuccess, ct);

        var orderCode = root.GetProperty("OrderCode").GetString();
        var phoneNumber = root.GetProperty("PhoneNumber").GetString();
        var description = root.GetProperty("Description").GetString();

        string mismatchLog = $"📸 گزارش مغایرت عکس و محصول:\n" +
            $"کد سفارش: {orderCode}\n" +
            $"شماره تماس: {phoneNumber}\n" +
            $"توضیحات: {description}\n";

        var order = await LookupOrderAsync(orderCode, ct);
        mismatchLog += order + userBaleUsername + "\n #mismatch";

        await botClient.SendMessageAsync(targetChatId, mismatchLog, ct);
    }

    private async Task HandleReturnedPackageAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, CancellationToken ct)
    {
        string messageSuccess = "✅ اطلاعات شما ثبت شد. پشتیبانی ما در اسرع وقت با شما تماس خواهد گرفت.";
        await botClient.SendMessageAsync(userChatId, messageSuccess, ct);

        var orderCode = root.GetProperty("OrderCode").GetString();
        var phoneNumber = root.GetProperty("PhoneNumber").GetString();
        var trackingCode = root.GetProperty("TrackingCode").GetString();

        string returnedLog = $"📦 گزارش بسته برگشت خورده:\n" +
            $"کد سفارش: {orderCode}\n" +
            $"شماره تماس: {phoneNumber}\n" +
            $"کد رهگیری پست: {trackingCode}\n";

        var order = await LookupOrderAsync(orderCode, ct);
        returnedLog += order + userBaleUsername + "\n #returned";

        await botClient.SendMessageAsync(targetChatId, returnedLog, ct);
    }

    private async Task HandleWholesaleAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, CancellationToken ct)
    {
        string messageSuccess = "✅ درخواست عمده شما ثبت شد. به زودی با شما تماس گرفته خواهد شد.";
        await botClient.SendMessageAsync(userChatId, messageSuccess, ct);

        var phoneNumber = root.GetProperty("PhoneNumber").GetString();
        var description = root.GetProperty("Description").GetString();

        string wholesaleLog = $"📦 درخواست سفارش عمده جدید:\n" +
            $"شماره تماس: {phoneNumber}\n" +
            $"توضیحات: {description}" +
            userBaleUsername + "\n #wholesale";

        await botClient.SendMessageAsync(targetChatId, wholesaleLog, ct);
    }

    private async Task HandleNoOrderCodeAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, CancellationToken ct)
    {
        string messageSuccess = "✅ اطلاعات شما ثبت شد. پشتیبانی ما کد سفارش شما را پیدا کرده و به شما اطلاع خواهد داد.";
        await botClient.SendMessageAsync(userChatId, messageSuccess, ct);

        var fullName = root.GetProperty("FullName").GetString();
        var phoneNumber = root.GetProperty("PhoneNumber").GetString();
        var orderAmount = root.GetProperty("OrderAmount").GetString();
        var paymentDate = root.GetProperty("PaymentDate").GetString();

        string noCodeLog = $"🔍 درخواست یافتن کد سفارش:\n" +
            $"نام و نام خانوادگی: {fullName}\n" +
            $"شماره تماس: {phoneNumber}\n" +
            $"مبلغ سفارش: {orderAmount}\n" +
            $"تاریخ پرداخت: {paymentDate}" +
            userBaleUsername + "\n #nocode";

        await botClient.SendMessageAsync(targetChatId, noCodeLog, ct);
    }

    private async Task HandleFailedPaymentAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, CancellationToken ct)
    {
        string messageSuccess = "✅ اطلاعات شما ثبت شد. پشتیبانی ما در اسرع وقت موضوع را بررسی خواهد کرد.";
        await botClient.SendMessageAsync(userChatId, messageSuccess, ct);

        var phoneNumber = root.GetProperty("PhoneNumber").GetString();
        var orderAmount = root.GetProperty("OrderAmount").GetString();
        var paymentDate = root.GetProperty("PaymentDate").GetString();
        var description = root.GetProperty("Description").GetString();

        string failedPaymentLog = $"💳 گزارش پرداخت ناموفق:\n" +
            $"شماره تماس: {phoneNumber}\n" +
            $"مبلغ: {orderAmount}\n" +
            $"تاریخ پرداخت: {paymentDate}\n" +
            $"توضیحات: {description}" +
            userBaleUsername + "\n #failedpayment";

        await botClient.SendMessageAsync(targetChatId, failedPaymentLog, ct);
    }

    private async Task HandleDelayedDeliveryAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, CancellationToken ct)
    {
        string messageSuccess = "✅ اطلاعات شما ثبت شد. پشتیبانی ما پیگیری لازم را انجام خواهد داد.";
        await botClient.SendMessageAsync(userChatId, messageSuccess, ct);

        var orderCode = root.GetProperty("OrderCode").GetString();
        var phoneNumber = root.GetProperty("PhoneNumber").GetString();
        var fullName = root.GetProperty("FullName").GetString();
        var postalCode = root.GetProperty("PostalCode").GetString();

        string delayedLog = $"⏰ گزارش تاخیر در تحویل (بالای ۸ روز کاری):\n" +
            $"کد سفارش: {orderCode}\n" +
            $"نام و نام خانوادگی: {fullName}\n" +
            $"شماره تماس: {phoneNumber}\n" +
            $"کد پستی: {postalCode}\n";

        var order = await LookupOrderAsync(orderCode, ct);
        delayedLog += order + userBaleUsername + "\n #delayed";

        await botClient.SendMessageAsync(targetChatId, delayedLog, ct);
    }

    private async Task HandleWrongSizeAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, CancellationToken ct)
    {
        string messageSuccess = "✅ اطلاعات شما ثبت شد. پشتیبانی ما در اسرع وقت با شما تماس خواهد گرفت.";
        await botClient.SendMessageAsync(userChatId, messageSuccess, ct);

        var orderCode = root.GetProperty("OrderCode").GetString();
        var phoneNumber = root.GetProperty("PhoneNumber").GetString();
        var description = root.GetProperty("Description").GetString();

        string wrongSizeLog = $"📏 گزارش سایز نامناسب:\n" +
            $"کد سفارش: {orderCode}\n" +
            $"شماره تماس: {phoneNumber}\n" +
            $"توضیحات: {description}\n";

        var order = await LookupOrderAsync(orderCode, ct);
        wrongSizeLog += order + userBaleUsername + "\n #wrongsize";

        await botClient.SendMessageAsync(targetChatId, wrongSizeLog, ct);
    }

    private async Task<string> LookupOrderAsync(string? orderCode, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(orderCode))
            return "";

        var order = await dbContext.CustomerOrder
            .Include(o => o.OrderStatus)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode, ct);

        if (order is not null)
        {
            return "\n" +
                $"📦 سفارش «{order.OrderCode}»:\n" +
                $"وضعیت: {order.OrderStatus.Title}\n" +
                $"آخرین به‌روزرسانی: {PersianCalendarTools.GregorianToPersian(order.UpdatedAt)} {order.UpdatedAt:HH:mm}";
        }
        else
        {
            return "\n" + $"❌ سفارشی با کد «{orderCode}» یافت نشد.";
        }
    }
}

