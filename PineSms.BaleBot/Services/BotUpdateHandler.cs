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
    private const string SupportWaitNotice = "\nلطفاً تا ۷۲ ساعت کاری آینده صبوری کنید. درخواست شما بررسی می‌شود. لطفاً دیگر پیام ندهید، پاسخ‌گویی بر اساس آخرین پیام‌ها انجام می‌شود.";

    private readonly BaleBotClient botClient;
    private readonly PineSmsDbContext dbContext;
    private readonly IChatAgentService agentService;
    private readonly ChatSessionStore sessionStore;
    private readonly BotChatMessageQueue chatMessageQueue;
    private readonly PhotoMessageStore photoMessageStore;
    private readonly ILogger<BotUpdateHandler> logger;
    private readonly List<long> chatIds = new()
    {
        6318588996,5715522360,6215427121,6137308408,
        5518881690,5000226193,5225037607,6178785306,
        5477856928,5172013155,5249048339
    };

    public BotUpdateHandler(
        BaleBotClient botClient,
        PineSmsDbContext dbContext,
        IChatAgentService agentService,
        ChatSessionStore sessionStore,
        BotChatMessageQueue chatMessageQueue,
        PhotoMessageStore photoMessageStore,
        ILogger<BotUpdateHandler> logger)
    {
        this.botClient = botClient;
        this.dbContext = dbContext;
        this.agentService = agentService;
        this.sessionStore = sessionStore;
        this.chatMessageQueue = chatMessageQueue;
        this.photoMessageStore = photoMessageStore;
        this.logger = logger;
    }

    public async Task HandleAsync(BaleUpdate update, CancellationToken ct)
    {
        var message = update.Message;
        if (message is null)
            return;

        bool hasPhoto = message.Photo is { Length: > 0 };
        bool hasText = !string.IsNullOrWhiteSpace(message.Text);
        bool hasCaption = !string.IsNullOrWhiteSpace(message.Caption);

        // Ignore messages with no text, caption, or photo content
        if (!hasText && !hasPhoto && !hasCaption)
            return;

        var chatId = message.Chat.Id;

        if (chatIds.Contains(message.Chat.Id))
        {
            return;
        }

        var username = message.From?.Username;

        if (string.IsNullOrEmpty(username))
        {
            await botClient.SendMessageAsync(chatId, """
                همراه عزیز مزون آناناس
                نام کاربری (آیدی) بله شما در دسترس نیست
                جهت امکان پذیر شدن ارتباط با شما
                لطفا نام کاربری (آیدی) خود را ست کنید
                یا اگر ست کرده اید، در دسترسی عمومی قرار دهید
                """, ct);

            return;
        }

        logger.LogInformation("Update {UpdateId}: chat={ChatId} hasPhoto={HasPhoto}", update.UpdateId, chatId, hasPhoto);

        // When the user sends a photo, store it for later forwarding
        if (hasPhoto)
        {
            // Use the highest-resolution variant (last element in the array)
            var bestPhoto = message.Photo![^1];
            photoMessageStore.StorePhoto(chatId, message.MessageId);
            logger.LogInformation("Stored photo message_id={MessageId} for chat={ChatId} (file_id={FileId})",
                message.MessageId, chatId, bestPhoto.FileId);
        }

        // A photo-only message (no text, no caption) is stored above and nothing else.
        // We must NOT forward it to the AI — the user may be sending several photos in a row
        // and each would independently trigger an AI response, causing TakePhotos() to drain
        // the store prematurely so later photos are never forwarded to support.
        if (hasPhoto && !hasText && !hasCaption)
            return;

        // Build the text that will be forwarded to the AI.
        // For a photo+caption message the photo is already stored above, so we pass only
        // the raw caption — no wrapper prefix — to avoid confusing the AI into thinking
        // it needs to handle a fresh photo separately from the ones already queued.
        string text;
        if (hasText)
            text = message.Text!.Trim();
        else
            text = message.Caption!.Trim();

        // If the user has queued photos that the AI does not yet know about, append a
        // system note so the AI sets HasPhoto:true in the FEEDBACK block.
        var pendingPhotoCount = photoMessageStore.PeekPhotos(chatId).Count;
        if (pendingPhotoCount > 0)
            text += $"\n[سیستم: کاربر {pendingPhotoCount} تصویر ارسال کرده است]";

        // Enqueue user message for background persistence (fire-and-forget)
        chatMessageQueue.TryEnqueue(new BotChatMessageEntry(username, chatId, text, IsFromBot: false, DateTime.UtcNow));

        // /start → reset session so user always gets a fresh greeting
        if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            sessionStore.RemoveSession(chatId);
        }

        var existingSession = sessionStore.GetSession(chatId);
        var response = await agentService.SendWithSessionAsync(existingSession, text);
        sessionStore.SetSession(chatId, response.SerializedSession);

        var orderCodes = new List<string>();
        var visibleOrderCodes = ResponseBlockTools.StripOrderCodeBlocks(response.ResponseText, orderCodes);
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
                        (!string.IsNullOrEmpty(order.PostalTrackingCode) ? $"کد مرسوله پستی: {order.PostalTrackingCode}\n" : "") +
                        $" کد ۲۴ رقمیو بزن تو سایت پست https://tracking.post.ir/ از وضعیت بسته باخبر شو");
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
                await SendAndEnqueueBotReplyAsync(chatId, username, visibleOrderCodes, ct);

            // A FEEDBACK block may accompany the ORDER_CODE block (e.g. DelayedDelivery
            // where the AI checks the order and escalates in the same turn). Process it too.
            if (!string.IsNullOrEmpty(feedbackJson))
                await HandleFeedbackAsync(chatId, feedbackJson, username, ct);
        }
        else if (!string.IsNullOrEmpty(feedbackJson))
        {
            await HandleFeedbackAsync(chatId, feedbackJson, username, ct);
        }
        else
        {
            await SendAndEnqueueBotReplyAsync(chatId, username, visibleOrderCodes, ct);
        }
    }

    /// <summary>Sends a bot reply to the user and enqueues it for background persistence.</summary>
    private async Task SendAndEnqueueBotReplyAsync(long userChatId, string username, string text, CancellationToken ct)
    {
        await botClient.SendMessageAsync(userChatId, text, ct);
        chatMessageQueue.TryEnqueue(new BotChatMessageEntry(username, userChatId, text, IsFromBot: true, DateTime.UtcNow));
    }

    private async Task HandleFeedbackAsync(long userChatId, string feedbackJson, string username, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(username))
        {
            const string noUsernameMsg = "دوست عزیز مزون آناناس، لطفاً نام کاربری خود را در بله تنظیم کنید و در دسترس قرار دهید تا بتوانیم به شما پاسخ دهیم.";
            await botClient.SendMessageAsync(userChatId, noUsernameMsg, ct);
            chatMessageQueue.TryEnqueue(new BotChatMessageEntry(userChatId.ToString(), userChatId, noUsernameMsg, IsFromBot: true, DateTime.UtcNow));
            return;
        }

        using var feedbackDoc = JsonDocument.Parse(feedbackJson);
        var root = feedbackDoc.RootElement;

        // Extract feedback type to determine routing
        if (!root.TryGetProperty("Type", out var typeProperty))
        {
            logger.LogWarning("Feedback JSON missing 'Type' field");
            return;
        }

        string feedbackType = typeProperty.GetString() ?? string.Empty;

        // Read target chat ID directly from the JSON generated by the AI
        if (!root.TryGetProperty("TargetChatId", out var chatIdProperty) ||
            !chatIdProperty.TryGetInt64(out long targetChatId))
        {
            logger.LogWarning("Feedback JSON missing or invalid 'TargetChatId' for type: {FeedbackType}", feedbackType);
            return;
        }

        // Skip routing if chat ID is not configured (0 means placeholder)
        if (targetChatId == 0)
        {
            logger.LogWarning("Chat ID not configured for feedback type: {FeedbackType}", feedbackType);
            const string unconfiguredMsg = "✅ اطلاعات شما ثبت شد. پشتیبانی ما در بله در اسرع وقت به شما پیام می‌دهد." + SupportWaitNotice;
            await botClient.SendMessageAsync(userChatId, unconfiguredMsg, ct);
            chatMessageQueue.TryEnqueue(new BotChatMessageEntry(username, userChatId, unconfiguredMsg, IsFromBot: true, DateTime.UtcNow));
            return;
        }

        string userBaleUsername = $"\n کاربری: @{username}";

        // Route to appropriate handler based on feedback type
        switch (feedbackType)
        {
            case "Satisfaction":
                await HandleSatisfactionAsync(userChatId, targetChatId, root, userBaleUsername, username, ct);
                break;

            case "Complaint":
                await HandleComplaintAsync(userChatId, targetChatId, root, userBaleUsername, username, ct);
                break;

            case "DefectiveProduct":
                await HandleDefectiveProductAsync(userChatId, targetChatId, root, userBaleUsername, username, ct);
                break;

            case "PhotoMismatch":
                await HandlePhotoMismatchAsync(userChatId, targetChatId, root, userBaleUsername, username, ct);
                break;

            case "ReturnedPackage":
                await HandleReturnedPackageAsync(userChatId, targetChatId, root, userBaleUsername, username, ct);
                break;

            case "Wholesale":
                await HandleWholesaleAsync(userChatId, targetChatId, root, userBaleUsername, username, ct);
                break;

            case "NoOrderCode":
                await HandleNoOrderCodeAsync(userChatId, targetChatId, root, userBaleUsername, username, ct);
                break;

            case "FailedPayment":
                await HandleFailedPaymentAsync(userChatId, targetChatId, root, userBaleUsername, username, ct);
                break;

            case "DelayedDelivery":
                await HandleDelayedDeliveryAsync(userChatId, targetChatId, root, userBaleUsername, username, ct);
                break;

            case "WrongSize":
                await HandleWrongSizeAsync(userChatId, targetChatId, root, userBaleUsername, username, ct);
                break;

            case "UnknownQuery":
                await HandleUnknownQueryAsync(userChatId, targetChatId, root, userBaleUsername, username, ct);
                break;

            default:
                logger.LogWarning("Unhandled feedback type: {FeedbackType}", feedbackType);
                break;
        }
    }

    private async Task HandleSatisfactionAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, string username, CancellationToken ct)
    {
        string messageSatisfactionSuccess = """
            مبارکتون باشه. خوشحالیم تونستیم پاسخ اعتمادتون رو بدیم. به امید دیدار مجدد در خرید های بعدی
            """;

        await botClient.SendMessageAsync(userChatId, messageSatisfactionSuccess, ct);
        chatMessageQueue.TryEnqueue(new BotChatMessageEntry(username, userChatId, messageSatisfactionSuccess, IsFromBot: true, DateTime.UtcNow));

        string orderCode = root.TryGetProperty("OrderCode", out var ocProp) ? ocProp.GetString() : "نامشخص";
        string description = root.TryGetProperty("Description", out var descProp) ? descProp.GetString() : "";

        string satisfactionLog = $"🌸 پیام رضایت جدید ثبت شد:\n" +
            $"کد سفارش: {orderCode}\n" +
            $"توضیحات: {description}" +
            userBaleUsername;

        await botClient.SendMessageAsync(targetChatId, satisfactionLog, CancellationToken.None);
    }

    private async Task HandleComplaintAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, string username, CancellationToken ct)
    {
        string messageComplaintSuccess = "📣 اطلاعات شما ثبت شد:\nپشتیبانی ما در بله در اسرع وقت به شما پیام می‌دهد." + SupportWaitNotice;
        await botClient.SendMessageAsync(userChatId, messageComplaintSuccess, ct);
        chatMessageQueue.TryEnqueue(new BotChatMessageEntry(username, userChatId, messageComplaintSuccess, IsFromBot: true, DateTime.UtcNow));

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
               .FirstOrDefaultAsync(o => o.OrderCode == orderCode, CancellationToken.None);

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

        await botClient.SendMessageAsync(targetChatId, complaintLog, CancellationToken.None);
    }

    private async Task HandleDefectiveProductAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, string username, CancellationToken ct)
    {
        string messageSuccess = "✅ اطلاعات شما ثبت شد. پشتیبانی ما در بله در اسرع وقت به شما پیام می‌دهد." + SupportWaitNotice;
        await botClient.SendMessageAsync(userChatId, messageSuccess, ct);
        chatMessageQueue.TryEnqueue(new BotChatMessageEntry(username, userChatId, messageSuccess, IsFromBot: true, DateTime.UtcNow));

        var orderCode = root.GetProperty("OrderCode").GetString();
        var phoneNumber = root.GetProperty("PhoneNumber").GetString();
        var fullName = root.TryGetProperty("FullName", out var fnProp) ? fnProp.GetString() : "نامشخص";
        var description = root.GetProperty("Description").GetString();
        bool hasPhoto = root.TryGetProperty("HasPhoto", out var photoEl) && photoEl.GetBoolean();

        string defectiveLog = $"⚠️ گزارش محصول معیوب/خراب:\n" +
            $"کد سفارش: {orderCode}\n" +
            $"نام و نام خانوادگی: {fullName}\n" +
            $"شماره تماس: {phoneNumber}\n" +
            $"توضیحات: {description}\n" +
            $"عکس ارسال شده: {(hasPhoto ? "بله" : "خیر")}\n";

        var order = await LookupOrderAsync(orderCode, ct);
        defectiveLog += order + userBaleUsername + "\n #defective";

        await botClient.SendMessageAsync(targetChatId, defectiveLog, CancellationToken.None);

        // Forward the user's photo(s) to the support chat when the AI confirmed one was received
        if (hasPhoto)
        {
            var storedMessageIds = photoMessageStore.TakePhotos(userChatId);
            if (storedMessageIds.Count > 0)
            {
                foreach (var msgId in storedMessageIds)
                    await botClient.ForwardMessageAsync(targetChatId, userChatId, msgId, CancellationToken.None);
            }
            else
            {
                logger.LogWarning("HasPhoto=true for DefectiveProduct but no stored photo found for chat {ChatId}", userChatId);
            }
        }
    }

    private async Task HandlePhotoMismatchAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, string username, CancellationToken ct)
    {
        string messageSuccess = "✅ اطلاعات شما ثبت شد. پشتیبانی ما در بله در اسرع وقت به شما پیام می‌دهد." + SupportWaitNotice;
        await botClient.SendMessageAsync(userChatId, messageSuccess, ct);
        chatMessageQueue.TryEnqueue(new BotChatMessageEntry(username, userChatId, messageSuccess, IsFromBot: true, DateTime.UtcNow));

        var orderCode = root.GetProperty("OrderCode").GetString();
        var phoneNumber = root.GetProperty("PhoneNumber").GetString();
        var fullName = root.TryGetProperty("FullName", out var fnProp) ? fnProp.GetString() : "نامشخص";
        var description = root.GetProperty("Description").GetString();
        bool hasPhoto = root.TryGetProperty("HasPhoto", out var photoEl) && photoEl.GetBoolean();

        string mismatchLog = $"📸 گزارش مغایرت عکس و محصول:\n" +
            $"کد سفارش: {orderCode}\n" +
            $"نام و نام خانوادگی: {fullName}\n" +
            $"شماره تماس: {phoneNumber}\n" +
            $"توضیحات: {description}\n" +
            $"عکس ارسال شده: {(hasPhoto ? "بله" : "خیر")}\n";

        var order = await LookupOrderAsync(orderCode, ct);
        mismatchLog += order + userBaleUsername + "\n #mismatch";

        await botClient.SendMessageAsync(targetChatId, mismatchLog, CancellationToken.None);

        // Forward the user's photo(s) to the support chat when the AI confirmed one was received
        if (hasPhoto)
        {
            var storedMessageIds = photoMessageStore.TakePhotos(userChatId);
            if (storedMessageIds.Count > 0)
            {
                foreach (var msgId in storedMessageIds)
                    await botClient.ForwardMessageAsync(targetChatId, userChatId, msgId, CancellationToken.None);
            }
            else
            {
                logger.LogWarning("HasPhoto=true for PhotoMismatch but no stored photo found for chat {ChatId}", userChatId);
            }
        }
    }

    private async Task HandleReturnedPackageAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, string username, CancellationToken ct)
    {
        string messageSuccess = "✅ اطلاعات شما ثبت شد. پشتیبانی ما در بله در اسرع وقت به شما پیام می‌دهد." + SupportWaitNotice;
        await botClient.SendMessageAsync(userChatId, messageSuccess, ct);
        chatMessageQueue.TryEnqueue(new BotChatMessageEntry(username, userChatId, messageSuccess, IsFromBot: true, DateTime.UtcNow));

        var orderCode = root.GetProperty("OrderCode").GetString();
        var phoneNumber = root.GetProperty("PhoneNumber").GetString();
        var fullName = root.TryGetProperty("FullName", out var fnProp) ? fnProp.GetString() : "نامشخص";
        var trackingCode = root.GetProperty("TrackingCode").GetString();

        string returnedLog = $"📦 گزارش بسته برگشت خورده:\n" +
            $"کد سفارش: {orderCode}\n" +
            $"نام و نام خانوادگی: {fullName}\n" +
            $"شماره تماس: {phoneNumber}\n" +
            $"کد رهگیری پست: {trackingCode}\n";

        var order = await LookupOrderAsync(orderCode, ct);
        returnedLog += order + userBaleUsername + "\n #returned";

        await botClient.SendMessageAsync(targetChatId, returnedLog, CancellationToken.None);
    }

    private async Task HandleWholesaleAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, string username, CancellationToken ct)
    {
        string messageSuccess = "✅ درخواست عمده شما ثبت شد. پشتیبانی ما در بله در اسرع وقت به شما پیام می‌دهد." + SupportWaitNotice;
        await botClient.SendMessageAsync(userChatId, messageSuccess, ct);
        chatMessageQueue.TryEnqueue(new BotChatMessageEntry(username, userChatId, messageSuccess, IsFromBot: true, DateTime.UtcNow));

        var phoneNumber = root.GetProperty("PhoneNumber").GetString();
        var fullName = root.TryGetProperty("FullName", out var fnProp) ? fnProp.GetString() : "نامشخص";
        var description = root.GetProperty("Description").GetString();

        string wholesaleLog = $"📦 درخواست سفارش عمده جدید:\n" +
            $"نام و نام خانوادگی: {fullName}\n" +
            $"شماره تماس: {phoneNumber}\n" +
            $"توضیحات: {description}" +
            userBaleUsername + "\n #wholesale";

        await botClient.SendMessageAsync(targetChatId, wholesaleLog, CancellationToken.None);
    }

    private async Task HandleNoOrderCodeAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, string username, CancellationToken ct)
    {
        string messageSuccess = "✅ اطلاعات شما ثبت شد. پشتیبانی ما پس از بررسی در بله به شما پیام می‌دهد." + SupportWaitNotice;
        await botClient.SendMessageAsync(userChatId, messageSuccess, ct);
        chatMessageQueue.TryEnqueue(new BotChatMessageEntry(username, userChatId, messageSuccess, IsFromBot: true, DateTime.UtcNow));

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

        await botClient.SendMessageAsync(targetChatId, noCodeLog, CancellationToken.None);
    }

    private async Task HandleFailedPaymentAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, string username, CancellationToken ct)
    {
        string messageSuccess = "✅ اطلاعات شما ثبت شد. پشتیبانی ما پس از بررسی در بله به شما پیام می‌دهد." + SupportWaitNotice;
        await botClient.SendMessageAsync(userChatId, messageSuccess, ct);
        chatMessageQueue.TryEnqueue(new BotChatMessageEntry(username, userChatId, messageSuccess, IsFromBot: true, DateTime.UtcNow));

        var phoneNumber = root.GetProperty("PhoneNumber").GetString();
        var fullName = root.TryGetProperty("FullName", out var fnProp) ? fnProp.GetString() : "نامشخص";
        var orderAmount = root.GetProperty("OrderAmount").GetString();
        var paymentDate = root.GetProperty("PaymentDate").GetString();
        var description = root.GetProperty("Description").GetString();

        string failedPaymentLog = $"💳 گزارش پرداخت ناموفق:\n" +
            $"نام و نام خانوادگی: {fullName}\n" +
            $"شماره تماس: {phoneNumber}\n" +
            $"مبلغ: {orderAmount}\n" +
            $"تاریخ پرداخت: {paymentDate}\n" +
            $"توضیحات: {description}" +
            userBaleUsername + "\n #failedpayment";

        await botClient.SendMessageAsync(targetChatId, failedPaymentLog, CancellationToken.None);
    }

    private async Task HandleDelayedDeliveryAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, string username, CancellationToken ct)
    {
        string messageSuccess = "✅ اطلاعات شما ثبت شد. پشتیبانی ما پس از پیگیری در بله به شما پیام می‌دهد." + SupportWaitNotice;
        await botClient.SendMessageAsync(userChatId, messageSuccess, ct);
        chatMessageQueue.TryEnqueue(new BotChatMessageEntry(username, userChatId, messageSuccess, IsFromBot: true, DateTime.UtcNow));

        var orderCode = root.GetProperty("OrderCode").GetString();
        var phoneNumber = root.GetProperty("PhoneNumber").GetString();
        var fullName = root.GetProperty("FullName").GetString();

        string delayedLog = $"⏰ گزارش تاخیر در تحویل (بالای ۸ روز کاری):\n" +
            $"کد سفارش: {orderCode}\n" +
            $"نام و نام خانوادگی: {fullName}\n" +
            $"شماره تماس: {phoneNumber}\n";

        var order = await LookupOrderAsync(orderCode, ct);
        delayedLog += order + userBaleUsername + "\n #delayed";

        await botClient.SendMessageAsync(targetChatId, delayedLog, CancellationToken.None);
    }

    private async Task HandleWrongSizeAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, string username, CancellationToken ct)
    {
        string messageSuccess = "✅ اطلاعات شما ثبت شد. پشتیبانی ما در بله در اسرع وقت به شما پیام می‌دهد." + SupportWaitNotice;
        await botClient.SendMessageAsync(userChatId, messageSuccess, ct);
        chatMessageQueue.TryEnqueue(new BotChatMessageEntry(username, userChatId, messageSuccess, IsFromBot: true, DateTime.UtcNow));

        var orderCode = root.GetProperty("OrderCode").GetString();
        var phoneNumber = root.GetProperty("PhoneNumber").GetString();
        var fullName = root.TryGetProperty("FullName", out var fnProp) ? fnProp.GetString() : "نامشخص";
        var description = root.GetProperty("Description").GetString();

        string wrongSizeLog = $"📏 گزارش سایز نامناسب:\n" +
            $"کد سفارش: {orderCode}\n" +
            $"نام و نام خانوادگی: {fullName}\n" +
            $"شماره تماس: {phoneNumber}\n" +
            $"توضیحات: {description}\n";

        var order = await LookupOrderAsync(orderCode, ct);
        wrongSizeLog += order + userBaleUsername + "\n #wrongsize";

        await botClient.SendMessageAsync(targetChatId, wrongSizeLog, CancellationToken.None);
    }

    private async Task HandleUnknownQueryAsync(long userChatId, long targetChatId, JsonElement root, string userBaleUsername, string username, CancellationToken ct)
    {
        string messageSuccess = "✅ پیام شما ثبت شد. پشتیبانی ما در بله در اسرع وقت به شما پیام می‌دهد." + SupportWaitNotice;
        await botClient.SendMessageAsync(userChatId, messageSuccess, ct);
        chatMessageQueue.TryEnqueue(new BotChatMessageEntry(username, userChatId, messageSuccess, IsFromBot: true, DateTime.UtcNow));

        var fullName = root.TryGetProperty("FullName", out var fnProp) ? fnProp.GetString() : "نامشخص";
        var description = root.TryGetProperty("Description", out var descProp) ? descProp.GetString() : "";

        string unknownLog = $"❓ درخواست نامشخص:\n" +
            $"نام و نام خانوادگی: {fullName}\n" +
            $"توضیحات: {description}" +
            userBaleUsername + "\n #unknown";

        await botClient.SendMessageAsync(targetChatId, unknownLog, CancellationToken.None);
    }

    private async Task<string> LookupOrderAsync(string? orderCode, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(orderCode))
            return "";

        orderCode = ResponseBlockTools.NormalizeDigits(orderCode);

        var order = await dbContext.CustomerOrder
            .Include(o => o.OrderStatus)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode, CancellationToken.None);

        if (order is not null)
        {
            return "\n" +
                 $"📦 سفارش «{order.OrderCode}»:\n" +
                        $"وضعیت: {order.OrderStatus.Title}\n" +
                        (!string.IsNullOrEmpty(order.PostalTrackingCode) ? $"کد مرسوله پستی: {order.PostalTrackingCode}\n" : "") +
                        $" کد ۲۴ رقمیو بزن تو سایت پست https://tracking.post.ir/ از وضعیت بسته باخبر شو";
        }
        else
        {
            return "\n" + $"❌ سفارشی با کد «{orderCode}» یافت نشد.";
        }
    }
}

