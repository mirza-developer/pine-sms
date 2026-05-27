# PineSms – سیستم مدیریت پیامک

سیستم مدیریت پیامک فارسی برای ارسال پیامک انبوه به مشتریان

---

## معرفی

**PineSms** یک سیستم مدیریت پیامک است که با معماری Clean Architecture در .NET 10 پیاده‌سازی شده است. این سیستم شامل یک API (ASP.NET Core Web API) و یک رابط کاربری (Blazor Server) می‌باشد که به صورت پاسخگو (Responsive) و با رابط کاربری فارسی طراحی شده است.

---

## ساختار پروژه

```
PineSms/
├── PineSms.Core/                 # لایه دامین – موجودیت‌ها، قراردادها، DTOها
├── PineSms.Persistence/          # لایه داده – EF Core، SQL Server، مخازن
├── PineSms.Identity/             # احراز هویت – JWT، Identity، seed داده
├── PineSms.Api/                  # REST API – کنترلرها
├── PineSms.UI/                   # رابط کاربری – Blazor Server
├── PineSms.BaleBot/              # سرویس ربات پیام‌رسان بله با قابلیت AI
├── PineSms.InstructionAnalyzer/  # ابزار تحلیل و بهبود دستورالعمل‌های AI
└── PineSms.slnx                  # فایل سولوشن
```

---

## پیش‌نیازها

- .NET 10 SDK
- SQL Server (یا SQL Server Express)
- Visual Studio 2022 یا VS Code

---

## نصب و راه‌اندازی

### ۱. کلون پروژه

```bash
git clone https://github.com/mirza-developer/pine-sms.git
cd pine-sms
```

### ۲. تنظیم رشته اتصال دیتابیس

فایل `PineSms.Api/appsettings.json` را ویرایش کنید:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PineSms;Trusted_Connection=True;TrustServerCertificate=True;",
    "IdentityConnection": "Server=localhost;Database=PineSmsIdentity;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### ۳. اجرای پروژه

**API:**
```bash
cd PineSms.Api
dotnet run
```

**UI:**
```bash
cd PineSms.UI
dotnet run
```

---

## راهنمای استفاده

### ورود به سیستم

- آدرس: `http://localhost:5000` (UI)
- نام کاربری: `admin`
- رمز عبور: `Admin@123`

---

## امکانات

### ۱. ورود به سیستم

سیستم دارای یک کاربر مدیر (`admin`) است که اطلاعات آن از طریق EF Core Seed Data در هنگام اجرای اول برنامه در دیتابیس ثبت می‌شود.

### ۲. موجودیت مشتری (`Customer`)

فیلدهای اجباری:
- **شماره موبایل** (`PhoneNumber`): ۱۰ رقم، شروع با ۹ (بدون صفر ابتدایی) – مثال: `9903063085`
- **تاریخ ثبت** (`SaveDate`): تاریخ و زمان ثبت (UTC)
- **شناسه کاربر ثبت‌کننده** (`SaveUserId`)
- **نوع ثبت** (`SaveType`): ۱=فرم، ۲=اکسل

فیلدهای اختیاری:
- نام (`Name`)
- جنسیت (`Gender`): ۱=مرد، ۲=زن
- سال تولد (`BirthYear`)
- تاریخ تولد (`BirthDate`)
- آخرین تاریخ استفاده (`LastUsageDate`)

### ۳. افزودن مشتری از طریق فرم

صفحه «افزودن مشتری» (`/customer/add`) یک فرم با اعتبارسنجی کامل دارد:
- بررسی فرمت شماره موبایل (۱۰ رقم، شروع با ۹)
- بررسی تکراری بودن شماره
- نمایش پیام خطا در صورت عدم اعتبار
- تاریخ تولد با انتخابگر تاریخ شمسی (PersianDatePicker)

### ۴. وارد کردن مشتریان از فایل اکسل

صفحه «ورود از اکسل» (`/customer/import`):
- فایل Excel با یک ستون، ردیف اول عنوان ستون (نادیده گرفته می‌شود)
- شماره‌ها باید ۱۰ رقم، شروع با ۹ باشند
- **مدیریت خطاها:**
  - شماره‌های نامعتبر (فرمت اشتباه) نمایش داده می‌شوند
  - شماره‌های تکراری (موجود در سیستم) نمایش داده می‌شوند
  - پس از نمایش خطاها، کاربر می‌تواند «نادیده گرفتن و ثبت بقیه» را انتخاب کند
  - برای شماره‌های تکراری: فقط `LastUsageDate` به‌روز می‌شود (ثبت مجدد نمی‌شوند)
  - شماره‌های معتبر و غیرتکراری در دیتابیس ثبت می‌شوند

### ۵. ارسال پیامک

صفحه «ارسال پیامک» (`/sms/send`):

**انتخاب بازه زمانی:**
- یک هفته اخیر
- دو هفته اخیر
- یک ماه اخیر
- یک فصل اخیر (سه ماه)
- یک سال اخیر
- بازه سفارشی (از تاریخ ... تا تاریخ ...)

**جدول مشتریان:**
- پس از جستجو، لیست مشتریان در جدول نمایش داده می‌شود
- همه ردیف‌ها به صورت پیش‌فرض انتخاب شده‌اند
- checkbox در هدر جدول برای انتخاب/لغو انتخاب همه ردیف‌ها
- می‌توان روی ردیف کلیک کرد یا checkbox را تغییر داد

**ارسال پیامک:**
- شماره فرستنده (از پنل ملی پیامک)
- متن پیامک
- دکمه ارسال نمایش می‌دهد به چند نفر ارسال می‌شود
- ارسال از طریق API ملی پیامک (MeliPayamak)
- لاگ ارسال با آرایه JSON از شماره‌ها و نتایج در دیتابیس ذخیره می‌شود

### ۶. کامپوننت PersianDatePicker

یک انتخابگر تاریخ شمسی کاملاً فارسی:
- نمایش تقویم شمسی با نام ماه‌های فارسی
- ناوبری ماهانه
- نمایش امروز (highlighted)
- نمایش تاریخ انتخاب‌شده

### ۷. سیستم اعلان‌ها (Notifications)

کامپوننت NotificationContainer برای نمایش پیام‌های موفقیت، خطا، هشدار و اطلاعات:
- موقعیت: گوشه بالا سمت راست
- حذف خودکار پس از ۵ ثانیه
- قابلیت بستن دستی

---

## معماری

### لایه Core (`PineSms.Core`)
- موجودیت‌ها: `Customer`, `SmsLog`, `IBaseEntity`
- قراردادها: `IAuthService`, `ICustomerService`, `ISmsService`
- DTOها: `GetUserLoginQuery/Result`, `InsertCustomerCommand`, `ImportCustomersCommand/Result`, `SendSmsCommand/Result`

### لایه Persistence (`PineSms.Persistence`)
- `PineSmsDbContext`: EF Core DbContext با auto-migration
- `CustomerRepository`: پیاده‌سازی `ICustomerService`
- `SmsRepository`: پیاده‌سازی `ISmsService` با HttpClient برای MeliPayamak

### لایه Identity (`PineSms.Identity`)
- `PineSmsIdentityContext`: Identity DbContext با seed داده برای admin
- `AuthService`: احراز هویت با JWT و SHA256 password hashing
- `ApplicationUser`: کاربر با فیلد `PersianName`
- Migrations برای ایجاد جدول‌های Identity

### لایه API (`PineSms.Api`)
- `AuthController`: POST `/api/auth/login`
- `CustomerController`: POST/GET `/api/customer`
- `SmsController`: POST `/api/sms/send`

### لایه UI (`PineSms.UI`)
- Blazor Server با رابط کاربری RTL (راست به چپ)
- صفحات: Login، Home، CustomerAdd، CustomerImport، SmsSend
- کامپوننت‌ها: PersianDatePicker، NotificationContainer
- Bootstrap RTL برای طراحی واکنش‌گرا

---

## API Documentation

### POST `/api/auth/login`
```json
{ "username": "admin", "password": "Admin@123" }
```
Response:
```json
{ "success": true, "token": "eyJ..." }
```

### POST `/api/customer`
```json
{ "phoneNumber": "9903063085", "name": "نام", "gender": 1, "birthYear": 1370, "birthDate": "1370/01/01" }
```

### POST `/api/customer/import`
```json
{ "phoneNumbers": ["9903063085", "9123456789"], "ignoreInvalid": false }
```

### GET `/api/customer/byrange?from=2024-01-01&to=2024-12-31`

### POST `/api/sms/send`
```json
{
  "customerIds": [1, 2, 3],
  "messageText": "متن پیامک",
  "fromNumber": "5000xxx",
  "dateRangeType": "LastMonth"
}
```

---

## تکنولوژی‌های استفاده‌شده

| تکنولوژی | نسخه | کاربرد |
|---|---|---|
| .NET | 10.0 | فریمورک اصلی |
| ASP.NET Core Web API | 10.0 | REST API |
| Blazor Server | 10.0 | رابط کاربری |
| Entity Framework Core | 10.0 | ORM |
| SQL Server | - | دیتابیس |
| ASP.NET Identity | 10.0 | مدیریت کاربران |
| JWT Bearer | 10.0 | احراز هویت |
| EPPlus | 7.7.0 | خواندن فایل Excel |
| Bootstrap | 5.x RTL | طراحی واکنش‌گرا |
