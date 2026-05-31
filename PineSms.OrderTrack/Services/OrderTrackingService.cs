using System.Net.Http.Json;

namespace PineSms.OrderTrack.Services;

public class OrderTrackingService(HttpClient httpClient)
{
    public async Task<(bool success, OrderTrackResult? result, string? error)> TrackAsync(string orderCode)
    {
        try
        {
            var response = await httpClient.GetAsync($"api/order/track/{Uri.EscapeDataString(orderCode.Trim())}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return (false, null, "لطفا صبوری کنید ۷۲ ساعت کاری از سفارشتون بگذره دوباره امتحان کنید");

            if (!response.IsSuccessStatusCode)
                return (false, null, $"خطا در دریافت اطلاعات (کد {(int)response.StatusCode})");

            var result = await response.Content.ReadFromJsonAsync<OrderTrackResult>();
            return (true, result, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"خطا در ارتباط با سرور: {ex.Message}");
        }
    }
}

public class OrderTrackResult
{
    public bool Found { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string StatusTitle { get; set; } = string.Empty;
    public string? PostalTrackingCode { get; set; }
    public DateTime UpdatedAt { get; set; }
}
