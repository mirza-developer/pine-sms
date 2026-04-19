using System.Net.Http.Headers;
using System.Net.Http.Json;
using PineSms.Core.Features.Account;
using PineSms.Core.Features.Customer;
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

    public async Task<List<PineSms.Core.Entities.Customer>?> GetCustomersByRangeAsync(DateTime from, DateTime to)
    {
        return await httpClient.GetFromJsonAsync<List<PineSms.Core.Entities.Customer>>(
            $"api/customer/byrange?from={from:yyyy-MM-ddTHH:mm:ss}&to={to:yyyy-MM-ddTHH:mm:ss}");
    }

    public async Task<SendSmsResult?> SendSmsAsync(SendSmsCommand command)
    {
        var response = await httpClient.PostAsJsonAsync("api/sms/send", command);
        return await response.Content.ReadFromJsonAsync<SendSmsResult>();
    }
}

public class MessageResponse
{
    public string Message { get; set; } = string.Empty;
}
