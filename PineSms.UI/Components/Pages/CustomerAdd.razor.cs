using Microsoft.AspNetCore.Components;
using PineSms.Core.Features.Customer;
using PineSms.UI.Services;

namespace PineSms.UI.Components.Pages;

public partial class CustomerAdd
{
    [Inject] private ApiClientService ApiClient { get; set; } = default!;
    [Inject] private AuthStateService AuthState { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;

    private InsertCustomerCommand command = new();
    private bool isLoading = false;

    private async Task HandleSubmit()
    {
        isLoading = true;
        try
        {
            var (success, message) = await ApiClient.InsertCustomerAsync(command);
            if (success)
            {
                NotificationService.ShowSuccess(message);
                command = new InsertCustomerCommand();
            }
            else
            {
                NotificationService.ShowError(message);
            }
        }
        catch
        {
            NotificationService.ShowError("خطا در اتصال به سرور");
        }
        finally
        {
            isLoading = false;
        }
    }

    private void Reset() => command = new InsertCustomerCommand();
}
