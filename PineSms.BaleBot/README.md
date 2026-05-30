# PineSms.BaleBot

A .NET 10 Worker Service that runs as a Bale Messenger chatbot for **Ananas Collection Boutique** (مزون اناناس کالکشن).  
It acts as a smart customer-support agent powered by an AI language model, processes customer messages, resolves order lookups from the database, and routes escalation requests to the correct human-support Bale chats.

---

## Table of Contents

1. [What This Project Does](#what-this-project-does)
2. [Architecture Overview](#architecture-overview)
3. [Message Handling Flow](#message-handling-flow)
4. [AI Agent & Instructions](#ai-agent--instructions)
5. [Feedback Types (Escalation Routing)](#feedback-types-escalation-routing)
6. [Photo Handling](#photo-handling)
7. [Services Reference](#services-reference)
8. [Workers Reference](#workers-reference)
9. [Models Reference](#models-reference)
10. [Configuration](#configuration)
11. [Running the Project](#running-the-project)
12. [Adding or Changing Bot Instructions](#adding-or-changing-bot-instructions)

---

## What This Project Does

`PineSms.BaleBot` continuously polls the [Bale Bot API](https://dev.bale.ai) (`tapi.bale.ai`) for incoming messages using long-polling (`getUpdates`).  
For every message it receives from a customer, it:

1. **Passes the message to an AI agent** that has been pre-loaded with a Persian-language customer-support instruction document (`Chat/chtbot-instructions-main.md`).
2. **Parses structured command blocks** from the AI's response:
   - `<<ORDER_CODE … >>` — triggers a live database lookup and appends the order status and postal tracking code to the reply.
   - `<<FEEDBACK … >>` — triggers routing of a structured notification to one of 11 predefined human-support Bale chat IDs based on the feedback type.
3. **Sends the final reply back to the customer** via `sendMessage`.
4. **Forwards photos** to the appropriate support chat when the AI flags `HasPhoto: true` (e.g. for defective-product reports).
5. **Persists all conversations** (both customer messages and bot replies) to the database via a fire-and-forget background queue.

---

## Architecture Overview

```
Bale API  ──────────────────────────────────────────────────────────────────────
  │                                                                             │
  │  getUpdates (long-poll, 30 s)                sendMessage / forwardMessage  │
  ▼                                                                             │
BaleBotWorker (BackgroundService)                                               │
  │                                                                             │
  │  dispatch each update                                                       │
  ▼                                                                             │
BotUpdateHandler (IBotUpdateHandler, Scoped)                                    │
  │                                                                             │
  ├── PhotoMessageStore  (store photo message IDs for later forwarding)        │
  │                                                                             │
  ├── IChatAgentService  (send text to AI, get back raw response)              │
  │       ├── ChatAgentService       (GitHub Models / OpenAI-compatible)       │
  │       └── ArvanChatAgentService  (ArvanCloud OAI-compatible)               │
  │                                                                             │
  ├── ResponseBlockTools  (parse <<ORDER_CODE>> and <<FEEDBACK>> blocks)       │
  │                                                                             │
  ├── PineSmsDbContext    (EF Core – order lookups)                            │
  │                                                                             │
  ├── BotChatMessageQueue (Channel<T> – fire-and-forget persistence)          │
  │       └── BotChatMessageSaverWorker (BackgroundService – drains queue)    │
  │                                                                             │
  └── BaleBotClient  (HTTP wrapper for Bale Bot API)  ───────────────────────►│
```

---

## Message Handling Flow

```
Incoming update
      │
      ├─ message is null?                          → ignore
      ├─ message has no text, caption, or photo?   → ignore
      ├─ chat ID is in internal support list?      → ignore (prevent loops)
      ├─ user has no Bale username?                → ask user to set one
      │
      ├─ message has a photo?
      │     └─ store (chatId → messageId) in PhotoMessageStore
      │
      ├─ build AI input:
      │     text message     → pass as-is
      │     photo + caption  → "[کاربر یک تصویر با توضیح ...]\n<caption>"
      │     photo only       → "[کاربر یک تصویر ارسال کرد]"
      │
      ├─ /start command?
      │     └─ remove session from ChatSessionStore (fresh greeting)
      │
      ├─ send text to IChatAgentService.SendWithSessionAsync()
      │     └─ uses/creates per-user session (conversation history)
      │
      ├─ parse AI response:
      │     StripOrderCodeBlocks()   → collect order codes
      │     StripFeedbackBlocks()    → collect feedback JSON
      │
      ├─ ORDER_CODE present?
      │     └─ look up each order in DB, append status + postal tracking code
      │
      └─ FEEDBACK present?
            └─ route to the correct handler (see Feedback Types below)
                  └─ if HasPhoto: true → ForwardMessageAsync() stored photos
```

---

## AI Agent & Instructions

The AI system prompt is loaded at startup from every `*.md` file inside the `Chat/` directory.  
Currently there is one file: **`Chat/chtbot-instructions-main.md`**.

This file defines:
- The bot's persona (a polite Persian-language boutique support assistant).
- All in-scope and out-of-scope topics.
- Exact response scripts for each workflow.
- The JSON templates for `<<ORDER_CODE>>` and `<<FEEDBACK>>` blocks.
- A 12-section knowledge base (products, shipping, policies, etc.).

**To modify the bot's behavior, edit `Chat/chtbot-instructions-main.md`.**  
The file is automatically copied to the output directory at build time (`CopyToOutputDirectory: Always`).

### AI Provider Switch

Set `AiProvider` in `appsettings.json`:

| Value | Implementation | Backend |
|---|---|---|
| `github` *(default)* | `ChatAgentService` | GitHub Models / any OpenAI-compatible endpoint |
| `arvan` | `ArvanChatAgentService` | ArvanCloud OAI-compatible REST API |

Both implementations share the same `IChatAgentService` interface and load the same instruction files.

---

## Feedback Types (Escalation Routing)

When the AI cannot resolve an issue itself, it emits a `<<FEEDBACK … >>` block containing a JSON object with a `Type` field and a `TargetChatId`. `BotUpdateHandler` routes the notification accordingly:

| Type | Description | Target Chat |
|---|---|---|
| `Satisfaction` | Customer appreciation / positive review | 6318588996 |
| `Complaint` | Unresolved complaint after KB attempt | 5715522360 |
| `DefectiveProduct` | Torn / stained / broken item (+ optional photo forward) | 6215427121 |
| `PhotoMismatch` | Product doesn't match website photo | 6137308408 |
| `ReturnedPackage` | Package returned / delivered to sender | 5518881690 |
| `Wholesale` | Wholesale order inquiry (6+ pieces) | 5000226193 |
| `NoOrderCode` | Customer lost their order code | 5225037607 |
| `UnknownQuery` | Anything outside the knowledge base | 6178785306 |
| `FailedPayment` | Payment deducted but order not confirmed | 5477856928 |
| `DelayedDelivery` | Order not arrived after 8+ business days | 5172013155 |
| `WrongSize` | Size doesn't fit | 5249048339 |

---

## Photo Handling

When a customer sends a photo (e.g. to show a defective product):

1. `BotUpdateHandler` detects `message.Photo != null` and calls `PhotoMessageStore.StorePhoto(chatId, messageId)`.
2. The message text sent to the AI is synthesised as a Persian placeholder so the AI knows a photo was attached.
3. When the AI generates a `DefectiveProduct` FEEDBACK block with `"HasPhoto": true`, the handler:
   - Calls `PhotoMessageStore.TakePhotos(chatId)` to retrieve stored message IDs.
   - Calls `BaleBotClient.ForwardMessageAsync(targetChatId, userChatId, messageId)` for each photo.
4. `PhotoMessageStoreCleanupWorker` runs every 5 minutes and evicts any photo entries older than 5 minutes (`PhotoMessageStore.EntryTtl`) to prevent memory growth.

---

## Services Reference

| Class | Lifetime | Responsibility |
|---|---|---|
| `BaleBotClient` | Singleton | HTTP wrapper for `sendMessage`, `forwardMessage`, `getUpdates` |
| `ChatSessionStore` | Singleton | In-memory map of `chatId → serialized session JSON` (per-user conversation state) |
| `PhotoMessageStore` | Singleton | In-memory map of `chatId → [messageId, …]` for pending photo forwards (TTL: 5 min) |
| `BotChatMessageQueue` | Singleton | `Channel<BotChatMessageEntry>` used as a fire-and-forget write buffer |
| `IChatAgentService` | Singleton | AI provider abstraction (`ChatAgentService` or `ArvanChatAgentService`) |
| `BotUpdateHandler` | Scoped | Core message dispatch logic (one instance per update) |

---

## Workers Reference

| Class | Type | Responsibility |
|---|---|---|
| `BaleBotWorker` | `BackgroundService` | Long-polls `getUpdates` (30 s timeout), dispatches each update concurrently to `IBotUpdateHandler` using dual-semaphore concurrency control |
| `BotChatMessageSaverWorker` | `BackgroundService` | Drains `BotChatMessageQueue` and persists each entry to the `BotChatMessage` table |
| `PhotoMessageStoreCleanupWorker` | `BackgroundService` | Periodically evicts expired photo entries from `PhotoMessageStore` |

---

## Concurrent Processing Model

`BaleBotWorker` processes updates from the same `getUpdates` batch concurrently using a **dual-semaphore** strategy.

### Why concurrency is needed

Each update requires at minimum one AI API call (1–5 s round-trip). Without concurrency every user in a batch waits behind every other user. With 10 simultaneous users, the last user would wait up to 50 s before their message is even sent to the AI.

### Dual-semaphore design

```
getUpdates batch  →  [update A (user 1), update B (user 2), update C (user 1), ...]
                             │                   │                   │
                             ▼                   ▼                   ▼
                      ProcessUpdateAsync   ProcessUpdateAsync   ProcessUpdateAsync
                             │                   │                   │
                    ┌────────▼────────┐          │          ┌────────▼────────┐
                    │ globalSemaphore │          │          │ globalSemaphore │
                    │    (cap = 10)   │          │          │    (cap = 10)   │
                    └────────┬────────┘          │          └────────┬────────┘
                             │                   │                   │
                    ┌────────▼────────┐          │          ┌────────▼────────┐
                    │ perUserSemaphore│          │          │ perUserSemaphore│
                    │  user 1 (cap=1) │          │          │  user 1 (cap=1) │◄── waits for A
                    └────────┬────────┘          │          └────────┬────────┘
                             │                   │                   │
                         handler A           handler B           handler C
                         (user 1)            (user 2)         (user 1, queued)
```

| Semaphore | Capacity | Purpose |
|---|---|---|
| `globalSemaphore` | 10 | Caps total in-flight handlers across all users. Prevents thread-pool exhaustion, DB connection-pool exhaustion, and runaway memory growth under load. |
| `perUserSemaphores[chatId]` | 1 | Serialises all messages from the **same** user. Guarantees that if a user sends two messages quickly, their second message is processed only after the first is fully complete — preserving AI session ordering and preventing mixed-up responses. |

### Offset safety

Offset advancement (`offset = updateId + 1`) is performed **synchronously** in a dedicated loop **before** any concurrent task is launched. This means:
- No update is ever double-processed even if a handler throws.
- The next `getUpdates` call uses the correct offset regardless of handler execution order.

### Cancellation token discipline

`stoppingToken` (the host shutdown token) is used as follows:

| Operation | Token used | Reason |
|---|---|---|
| `GetUpdatesAsync` | `stoppingToken` | Must stop polling when host shuts down |
| `globalSemaphore.WaitAsync` | `stoppingToken` | Must not block shutdown |
| `perUserSemaphore.WaitAsync` | `stoppingToken` | Must not block shutdown |
| `handler.HandleAsync` → user-facing `SendMessageAsync` | `stoppingToken` (as `ct`) | Acceptable to cancel; user hasn't received a reply yet |
| `handler.HandleAsync` → group `SendMessageAsync` | `CancellationToken.None` | **Must complete** — user already received their confirmation; dropping the group notification would lose the support ticket |
| `handler.HandleAsync` → `ForwardMessageAsync` (photos) | `CancellationToken.None` | Same reason as group sends |
| `LookupOrderAsync` → `FirstOrDefaultAsync` | `CancellationToken.None` | DB read must not be abandoned mid-flight; abandoning it would leave the message half-built |

### Tuning

`MaxConcurrentUpdates` is defined as a constant in `BaleBotWorker.cs`. The value `10` is appropriate for a boutique-scale bot. Increase it only if profiling shows the AI calls are the bottleneck and server resources (DB connections, memory) support more concurrency.

---

## Models Reference

| Class | Description |
|---|---|
| `BaleUpdate` | A single update from `getUpdates` |
| `BaleMessage` | A Bale message (text, caption, photo array, sender, chat) |
| `BaleUser` | Sender info (id, first name, last name, username) |
| `BaleChat` | Chat info (id, type) |
| `PhotoSize` | One resolution variant of a photo (`file_id`, `width`, `height`, `file_size`) |
| `BaleApiResponse<T>` | Generic wrapper for all Bale API responses (`ok`, `result`) |
| `BaleSendMessageRequest` | Request body for `sendMessage` |
| `BaleForwardMessageRequest` | Request body for `forwardMessage` |

---

## Configuration

All settings live in `appsettings.json` (and user secrets for sensitive values).

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "<SQL Server or SQLite connection string>"
  },
  "DatabaseProvider": "SqlServer",   // or "Sqlite"

  "BaleMessenger": {
    "BaseUrl": "https://tapi.bale.ai/",
    "Token": "<your Bale bot token>"
  },

  "AiProvider": "github",            // or "arvan"

  // Used when AiProvider = "github"
  "AiAgent": {
    "ApiKey": "<GitHub Models / OpenAI API key>",
    "Model": "gpt-4.1",
    "Endpoint": "https://models.github.ai/inference"
  },

  // Used when AiProvider = "arvan"
  "ArvanAiAgent": {
    "ApiKey": "<ArvanCloud API key>",
    "Model": "GPT-OSS-120B",
    "Endpoint": "https://arvancloudai.ir/gateway/models/GPT-OSS-120B/<KEY>/v1"
  },

  "Seq": {
    "ServerUrl": "http://localhost:5341"   // structured log sink (optional)
  }
}
```

---

## Running the Project

```bash
# Build the whole solution
dotnet build PineSms.slnx

# Run in development (SQLite mode – no SQL Server needed)
cd PineSms.BaleBot
dotnet run
```

The service can also be installed as a **Windows Service** via `sc.exe` or the .NET publish + `--service` pattern (`Microsoft.Extensions.Hosting.WindowsServices` is already configured).

---

## Adding or Changing Bot Instructions

1. Edit (or add a new `*.md` file to) `PineSms.BaleBot/Chat/`.
2. Rebuild — new files are auto-copied to the output directory.
3. Restart the service; `InitAsync()` reloads all `*.md` files on startup.

> **Tip for future agents:** The instruction file `chtbot-instructions-main.md` is the single source of truth for the bot's Persian-language behavior, knowledge base, and all FEEDBACK routing rules. Any behavioral change should start there, then verify that `BotUpdateHandler` handles the corresponding FEEDBACK `Type` (add a new `case` in the `switch` and a new private `HandleXxxAsync` method if needed).
