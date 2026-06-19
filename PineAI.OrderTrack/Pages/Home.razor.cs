using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PineAI.OrderTrack.Extensions;
using PineAI.OrderTrack.Services;

namespace PineAI.OrderTrack.Pages;
public partial class Home
{
    private string orderCode = string.Empty;
    private bool isLoading;
    private string? errorMessage;
    private OrderTrackResult? result;

    [Inject] OrderTrackingService TrackingService { get; set;  }

    public async Task TrackOrder()
    {
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            return; 
        }

        isLoading = true;
      
        errorMessage = null;
       
        result = null;

        var (success, trackResult, error) = await TrackingService.TrackAsync(orderCode.ToEnglishDigits());

        if (success)
        {
            result = trackResult;
        }
        else
        {
            errorMessage = error;
        }

        isLoading = false;
    }

    public async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await TrackOrder();
        }
    }
}
