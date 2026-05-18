using System.Net.Http.Headers;
using System.Net.Http.Json;
using PineSms.Core.Entities;
using PineSms.Core.Features.Account;
using PineSms.Core.Features.ApiKey;
using PineSms.Core.Features.Customer;
using PineSms.Core.Features.Order;
using PineSms.Core.Features.Sms;

namespace PineSms.UI.Services;

public class ApiClientService
{
    private readonly HttpClient httpClient;
    private string? token;

    public ApiClientService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public void SetToken(string jwtToken)
    {
        token = jwtToken;
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
    }

    public void ClearToken()
    {
        token = null;
        httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public bool HasToken => !string.IsNullOrEmpty(token);

    public async Task<GetUserLoginResult?> LoginAsync(GetUserLoginQuery query)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/login", query);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<GetUserLoginResult>();
        return new GetUserLoginResult { Success = false, Message = "خطا در ورود به سیستم" };
    }

    public async Task<(bool success, string message)> InsertCustomerAsync(InsertCustomerCommand command)
    {
        var response = await httpClient.PostAsJsonAsync("api/customer", command);
        if (response.IsSuccessStatusCode)
            return (true, "مشتری با موفقیت ثبت شد");
        var error = await response.Content.ReadFromJsonAsync<MessageResponse>();
        return (false, error?.Message ?? "خطا در ثبت مشتری");
    }

    public async Task<ImportCustomersResult?> ImportCustomersAsync(ImportCustomersCommand command)
    {
        var response = await httpClient.PostAsJsonAsync("api/customer/import", command);
        return await response.Content.ReadFromJsonAsync<ImportCustomersResult>();
    }

    public async Task<List<PineSms.Core.Entities.Customer>?> GetCustomersByRangeAsync(DateTime from, DateTime to, string? phonePrefix = null, bool? isTester = null)
    {
        var url = $"api/customer/byrange?from={from:yyyy-MM-ddTHH:mm:ss}&to={to:yyyy-MM-ddTHH:mm:ss}";
        if (!string.IsNullOrEmpty(phonePrefix))
            url += $"&phonePrefix={Uri.EscapeDataString(phonePrefix)}";
        if (isTester.HasValue)
            url += $"&isTester={isTester.Value.ToString().ToLowerInvariant()}";
        return await httpClient.GetFromJsonAsync<List<PineSms.Core.Entities.Customer>>(url);
    }

    public async Task<(PineSms.Core.Entities.Customer? customer, string? errorMessage)> GetCustomerByPhoneAsync(string phoneNumber)
    {
        var response = await httpClient.GetAsync($"api/customer/byphone/{phoneNumber}");
        if (response.IsSuccessStatusCode)
        {
            var customer = await response.Content.ReadFromJsonAsync<PineSms.Core.Entities.Customer>();
            return (customer, null);
        }
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return (null, "مشتری با این شماره یافت نشد");
        return (null, "خطا در جستجو");
    }

    public async Task<(bool success, string message)> UpdateCustomerAsync(UpdateCustomerCommand command)
    {
        var response = await httpClient.PutAsJsonAsync($"api/customer/{command.Id}", command);
        if (response.IsSuccessStatusCode)
        {
            var ok = await response.Content.ReadFromJsonAsync<MessageResponse>();
            return (true, ok?.Message ?? "اطلاعات مشتری به‌روزرسانی شد");
        }
        var error = await response.Content.ReadFromJsonAsync<MessageResponse>();
        return (false, error?.Message ?? "خطا در به‌روزرسانی مشتری");
    }

    public async Task<SendSmsResult?> SendSmsAsync(SendSmsCommand command)
    {
        var response = await httpClient.PostAsJsonAsync("api/sms/send", command);
        return await response.Content.ReadFromJsonAsync<SendSmsResult>();
    }

    public async Task<ScheduleSmsResult?> ScheduleSmsAsync(ScheduleSmsCommand command)
    {
        var response = await httpClient.PostAsJsonAsync("api/sms/schedule", command);
        return await response.Content.ReadFromJsonAsync<ScheduleSmsResult>();
    }

    public async Task<List<SmsSendJobDto>?> GetSmsJobsAsync()
    {
        return await httpClient.GetFromJsonAsync<List<SmsSendJobDto>>("api/sms/jobs");
    }

    public async Task<SmsSendJobDto?> GetSmsJobAsync(int id)
    {
        return await httpClient.GetFromJsonAsync<SmsSendJobDto>($"api/sms/jobs/{id}");
    }

    public async Task<BulkUpdateTrackingResult?> BulkUpdateTrackingAsync(BulkUpdateTrackingCommand command)
    {
        var response = await httpClient.PostAsJsonAsync("api/order/bulk-tracking", command);
        return await response.Content.ReadFromJsonAsync<BulkUpdateTrackingResult>();
    }

    // ── Order Statuses ──────────────────────────────────────────────────────
    public async Task<List<OrderStatus>?> GetOrderStatusesAsync()
    {
        return await httpClient.GetFromJsonAsync<List<OrderStatus>>("api/order/statuses");
    }

    public async Task<(bool success, string message)> UpsertOrderStatusAsync(UpsertOrderStatusCommand command)
    {
        var response = await httpClient.PostAsJsonAsync("api/order/statuses", command);
        if (response.IsSuccessStatusCode)
        {
            var ok = await response.Content.ReadFromJsonAsync<MessageResponse>();
            return (true, ok?.Message ?? "ذخیره شد");
        }
        var error = await response.Content.ReadFromJsonAsync<MessageResponse>();
        return (false, error?.Message ?? "خطا در ذخیره‌سازی");
    }

    public async Task<(bool success, string message)> DeleteOrderStatusAsync(int id)
    {
        var response = await httpClient.DeleteAsync($"api/order/statuses/{id}");
        if (response.IsSuccessStatusCode)
        {
            var ok = await response.Content.ReadFromJsonAsync<MessageResponse>();
            return (true, ok?.Message ?? "حذف شد");
        }
        var error = await response.Content.ReadFromJsonAsync<MessageResponse>();
        return (false, error?.Message ?? "خطا در حذف");
    }

    // ── API Keys ─────────────────────────────────────────────────────────────
    public async Task<List<ApiKey>?> GetApiKeysAsync()
    {
        return await httpClient.GetFromJsonAsync<List<ApiKey>>("api/apikey");
    }

    public async Task<CreateApiKeyResult?> CreateApiKeyAsync(CreateApiKeyCommand command)
    {
        var response = await httpClient.PostAsJsonAsync("api/apikey", command);
        return await response.Content.ReadFromJsonAsync<CreateApiKeyResult>();
    }

    public async Task<(bool success, string message)> DeleteApiKeyAsync(int id)
    {
        var response = await httpClient.DeleteAsync($"api/apikey/{id}");
        if (response.IsSuccessStatusCode)
        {
            var ok = await response.Content.ReadFromJsonAsync<MessageResponse>();
            return (true, ok?.Message ?? "حذف شد");
        }
        var error = await response.Content.ReadFromJsonAsync<MessageResponse>();
        return (false, error?.Message ?? "خطا در حذف");
    }
}

public class MessageResponse
{
    public string Message { get; set; } = string.Empty;
}
