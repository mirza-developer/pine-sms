# PineAI.BaleBot

## فارسی
PineAI.BaleBot یک **Worker Service بر پایه .NET 10** برای پیام‌رسان بله است که به‌عنوان عامل پشتیبانی هوشمند مزون **اناناس کالکشن** عمل می‌کند. این سرویس پیام‌های مشتریان را با کمک AI (GitHub Models / ArvanCloud) تحلیل می‌کند، سفارش‌ها را از پایگاه‌داده پیدا می‌کند، گفتگوها را در **SQL Server یا SQLite** ذخیره می‌کند، گزارش خرابی کالا را همراه با **فوروارد عکس** مدیریت می‌کند و در صورت نیاز مکالمه را به **۱۱ چت از پیش تعریف‌شده پشتیبانی انسانی** ارجاع می‌دهد. پردازش همزمان آپدیت‌ها نیز با طراحی **دو semaphore** انجام می‌شود.

## English
PineAI.BaleBot is a **.NET 10 Worker Service** chatbot for Bale Messenger and serves as an AI-powered customer support agent for **Ananas Collection Boutique**. It uses AI providers such as **GitHub Models** and **ArvanCloud** to process customer messages, resolve order lookups from the database, persist conversations to **SQL Server or SQLite**, forward photos for defective product reports, and escalate cases to **11 predefined human support chats** when needed. Incoming updates are handled concurrently with a **dual-semaphore** design.
