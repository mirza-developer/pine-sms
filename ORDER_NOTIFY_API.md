# PineSms — Order Notification API

This document explains how an external application can connect to the PineSms API to notify it of new or updated customer orders.

---

## Overview

The **Order Notification endpoint** allows any external system (e.g. an e-commerce store, ERP, or logistics platform) to push order status updates into PineSms. When an update arrives, PineSms will:

1. Register the customer (phone number) if they are not already in the database.
2. Look up the order status by its code.
3. Create the order if it is new, or update the status if the order already exists.
4. Send the customer a notification message via **Bale Messenger**.

---

## Authentication — API Keys

This endpoint is protected by **API key authentication**, not by the JWT tokens used for the admin UI. All other endpoints require a JWT bearer token and are not accessible via API keys.

### How to obtain an API key

1. Log in to the PineSms admin panel (UI).
2. Navigate to **تنظیمات → کلیدهای API** (Settings → API Keys).
3. Click **ایجاد کلید جدید** (Create New Key), enter a descriptive name and an expiry date, then click **ایجاد کلید**.
4. **Copy the generated key immediately** — it is only shown once and cannot be recovered later.
5. Store the key securely in your application's configuration (e.g. an environment variable or secrets manager).

### Sending the API key

Pass the key in the `X-Api-Key` HTTP request header with every call to the notify endpoint:

```
X-Api-Key: <your-api-key>
```

Requests without a valid, non-expired key will receive `401 Unauthorized`.

---

## Endpoint

### `POST /api/order/notify`

Notifies PineSms of a new or updated customer order.

#### Request headers

| Header | Required | Description |
|--------|----------|-------------|
| `X-Api-Key` | ✅ | A valid, non-expired API key issued from the admin panel |
| `Content-Type` | ✅ | `application/json` |

#### Request body (JSON)

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `customerPhoneNumber` | `string` | ✅ | Customer's mobile number — **10 digits, starting with `9`**, without the leading zero (e.g. `9123456789`) |
| `orderCode` | `string` | ✅ | Your unique identifier for the order (e.g. `ORD-20260428-001`). Max 128 characters. |
| `orderStatusCode` | `string` | ✅ | The code of an **existing** order status defined in PineSms (e.g. `SHIPPED`). Max 64 characters. |

> **Important:** Order status codes must first be created in the PineSms admin panel under **سفارشات → وضعیت‌های سفارش** before they can be referenced here.

#### Example request

```http
POST https://your-pinesms-host/api/order/notify
Content-Type: application/json
X-Api-Key: abc123XYZ_your_actual_key_here

{
  "customerPhoneNumber": "9123456789",
  "orderCode": "ORD-20260428-001",
  "orderStatusCode": "SHIPPED"
}
```

#### Success response — `200 OK`

```json
{
  "success": true,
  "message": "سفارش ثبت شد",
  "isNewCustomer": false,
  "isNewOrder": true,
  "notificationSent": true
}
```

| Field | Type | Description |
|-------|------|-------------|
| `success` | `bool` | `true` when the operation completed without errors |
| `message` | `string` | Human-readable result summary (in Persian) |
| `isNewCustomer` | `bool` | `true` if the customer was newly registered during this call |
| `isNewOrder` | `bool` | `true` if a new order was created; `false` if an existing order was updated |
| `notificationSent` | `bool` | `true` if the Bale Messenger notification was delivered successfully |

#### Error responses

| Status | Cause |
|--------|-------|
| `400 Bad Request` | The `orderStatusCode` does not match any status defined in PineSms, or required fields are missing/invalid |
| `401 Unauthorized` | The `X-Api-Key` header is absent, the key is invalid, or the key has expired |

**Example 400 body:**

```json
{
  "message": "وضعیت سفارش با کد 'UNKNOWN_STATUS' یافت نشد"
}
```

---

## Testing with Swagger UI

The API includes a built-in Swagger UI for interactive testing:

1. Open `https://your-pinesms-host/swagger` in your browser.
2. Click the **Authorize** button (🔓) at the top right.
3. Locate the **ApiKey** section, enter your key in the `Value` field, and click **Authorize**.
4. Expand **POST /api/order/notify**, click **Try it out**, fill in the request body, and click **Execute**.

---

## Pre-requisites checklist

Before calling the endpoint, ensure the following are in place:

- [ ] An API key has been created in the admin panel and is not expired.
- [ ] At least one **Order Status** with the intended `code` has been created in the admin panel.
- [ ] The Bale Messenger token has been configured in `appsettings.json` under `BaleMessenger:Token` (optional — notifications are skipped gracefully if not configured).

---

## Code examples

### curl

```bash
curl -X POST https://your-pinesms-host/api/order/notify \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: abc123XYZ_your_actual_key_here" \
  -d '{
    "customerPhoneNumber": "9123456789",
    "orderCode": "ORD-20260428-001",
    "orderStatusCode": "SHIPPED"
  }'
```

### Python (requests)

```python
import requests

url = "https://your-pinesms-host/api/order/notify"
headers = {
    "Content-Type": "application/json",
    "X-Api-Key": "abc123XYZ_your_actual_key_here"
}
payload = {
    "customerPhoneNumber": "9123456789",
    "orderCode": "ORD-20260428-001",
    "orderStatusCode": "SHIPPED"
}

response = requests.post(url, json=payload, headers=headers)
print(response.status_code, response.json())
```

### JavaScript (fetch)

```javascript
const response = await fetch("https://your-pinesms-host/api/order/notify", {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    "X-Api-Key": "abc123XYZ_your_actual_key_here"
  },
  body: JSON.stringify({
    customerPhoneNumber: "9123456789",
    orderCode: "ORD-20260428-001",
    orderStatusCode: "SHIPPED"
  })
});

const data = await response.json();
console.log(data);
```

### C# (HttpClient)

```csharp
using var client = new HttpClient();
client.DefaultRequestHeaders.Add("X-Api-Key", "abc123XYZ_your_actual_key_here");

var payload = new
{
    customerPhoneNumber = "9123456789",
    orderCode = "ORD-20260428-001",
    orderStatusCode = "SHIPPED"
};

var response = await client.PostAsJsonAsync(
    "https://your-pinesms-host/api/order/notify",
    payload);

var result = await response.Content.ReadFromJsonAsync<JsonElement>();
Console.WriteLine(result);
```

---

## Security recommendations

- **Rotate keys** periodically and set a reasonable expiry date.
- **Never** embed API keys in client-side code (browser JavaScript, mobile apps). Only call this endpoint from a trusted server-side backend.
- Use **HTTPS** at all times.
- If a key is compromised, delete it immediately from the admin panel — it takes effect instantly.
