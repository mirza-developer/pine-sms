using System.Net.Http.Json;
using System.Text.Json.Serialization;

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

            // Trim-safe: uses the source-generated JsonTypeInfo<OrderTrackResult>
            var result = await response.Content.ReadFromJsonAsync(
                OrderTrackResultContext.Default.OrderTrackResult);

            return (true, result, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"خطا در ارتباط با سرور: {ex.Message}");
        }
    }
}
