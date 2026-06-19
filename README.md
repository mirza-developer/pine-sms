# PineAI.BaleBot

## فارسی

`PineAI.BaleBot` یک **Worker Service مبتنی بر .NET 10** برای پیام‌رسان **بله** است که به‌عنوان هسته پشتیبانی هوشمند مزون **اناناس کالکشن** عمل می‌کند. این سرویس پیام‌های ورودی مشتریان را به‌صورت long-polling از Bale API دریافت می‌کند، آن‌ها را با استفاده از مدل‌های AI پردازش می‌کند، پاسخ نهایی را به کاربر برمی‌گرداند و در صورت نیاز، درخواست را به چت‌های پشتیبانی انسانی ارجاع می‌دهد.

از نظر فنی، ربات دارای معماری سرویس‌محور و چندبخشی است:

- دریافت و ارسال پیام از طریق `BaleBotClient`
- پردازش اصلی هر آپدیت در `BotUpdateHandler`
- نگه‌داری session مکالمه برای هر کاربر در `ChatSessionStore`
- پشتیبانی از دو ارائه‌دهنده AI از طریق `IChatAgentService`:
  - **GitHub Models**
  - **ArvanCloud OpenAI-compatible API**
- استخراج بلاک‌های ساختاریافته از پاسخ AI برای:
  - `<<ORDER_CODE>>` جهت استعلام سفارش از دیتابیس
  - `<<FEEDBACK>>` جهت ارجاع خودکار به تیم پشتیبانی
- ذخیره پیام‌ها در پایگاه داده با صف غیرهمزمان `BotChatMessageQueue`
- پشتیبانی از **SQL Server** و **SQLite** برای اجرا در محیط‌های مختلف

### قابلیت‌های اصلی

- پردازش پیام‌های متنی، کپشن و تصاویر
- جست‌وجوی وضعیت سفارش و کد رهگیری از دیتابیس
- فوروارد خودکار عکس‌ها برای سناریوهایی مثل کالای معیوب
- ارجاع خودکار موارد خارج از پاسخ‌گویی ربات به **۱۱ چت پشتیبانی از پیش تعریف‌شده**
- بارگذاری فایل‌های دستورالعمل ربات از پوشه `Chat/`
- ثبت و نگه‌داری تاریخچه گفتگوها برای تحلیل و پیگیری

### مدل پردازش همزمان

برای جلوگیری از تداخل پاسخ‌ها و در عین حال حفظ throughput مناسب، `BaleBotWorker` از یک طراحی **dual-semaphore** استفاده می‌کند:

- یک semaphore سراسری برای محدود کردن تعداد کل آپدیت‌های همزمان
- یک semaphore مجزا برای هر کاربر جهت حفظ ترتیب پیام‌های همان کاربر

این طراحی باعث می‌شود پیام‌های کاربران مختلف همزمان پردازش شوند، اما پیام‌های یک کاربر هرگز با هم قاطی نشوند.

---

## English

`PineAI.BaleBot` is a **.NET 10 Worker Service** for **Bale Messenger** that acts as the AI-powered support backend for **Ananas Collection Boutique**. It continuously polls the Bale Bot API, processes incoming customer messages through AI providers, builds structured replies, performs order lookups against the database, and escalates unresolved cases to human support chats when required.

Technically, the bot is built around a service-oriented processing pipeline:

- `BaleBotClient` wraps Bale API operations such as `getUpdates`, `sendMessage`, and `forwardMessage`
- `BotUpdateHandler` orchestrates update processing, reply composition, escalation, and persistence
- `ChatSessionStore` maintains per-user conversation state
- `IChatAgentService` abstracts multiple AI backends:
  - **GitHub Models**
  - **ArvanCloud OpenAI-compatible API**
- AI responses may emit structured blocks:
  - `<<ORDER_CODE>>` for live order-status and tracking lookup
  - `<<FEEDBACK>>` for escalation routing
- `BotChatMessageQueue` and background saver workers persist conversations asynchronously
- Database access supports both **SQL Server** and **SQLite**

### Core Behaviors

- Long-polling update consumption from Bale
- AI-assisted Persian-language customer support
- Database-backed order resolution
- Automatic forwarding of user photos for defective-product workflows
- Routing of unresolved or business-specific cases to **11 predefined human support chats**
- Instruction-driven behavior loaded from markdown files under `Chat/`
- Concurrent update handling with per-user ordering guarantees

### Build

```bash
dotnet build PineAI.slnx
```

### Technologies

| Technology | Version | Usage |
|---|---|---|
| .NET | 10.0 | Main runtime and worker host |
| Worker Service | .NET 10 | Background Bale bot service |
| Bale Bot API | - | Messaging transport |
| Entity Framework Core | 10.0 | Data access |
| SQL Server | - | Primary relational database option |
| SQLite | - | Lightweight/local database option |
| Microsoft.Extensions.AI | 10.3.0 | AI integration abstraction |
| OpenAI SDK | 2.8.0 | AI provider client integration |
| Microsoft.Agents.AI | 1.0.0-preview.260212.1 | Agent-oriented AI support |
| Serilog | 10.x | Structured logging |
| Seq | 9.0.0 sink | Centralized log sink |

### Related Projects in the Solution

- `PineAI.BaleBot` — Bale chatbot worker
- `PineAI.Persistence` — EF Core persistence layer
- `PineAI.Core` — shared domain/contracts
- `PineAI.Api` — API layer
- `PineAI.UI` — UI layer
- `PineAI.Identity` — authentication and identity services
