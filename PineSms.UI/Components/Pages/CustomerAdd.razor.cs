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
    private bool isSearching = false;

    // null  = no search performed yet
    // true  = customer found (update mode)
    // false = not found (insert mode)
    private bool? searchFound = null;
    private string searchMessage = string.Empty;

    // When non-null we are in update mode; holds the found customer's Id
    private int? foundCustomerId = null;

    private bool IsUpdateMode => foundCustomerId.HasValue;

    private async Task HandleSearch()
    {
        if (string.IsNullOrWhiteSpace(command.PhoneNumber))
        {
            searchFound = false;
            searchMessage = "شماره موبایل را وارد کنید";
            return;
        }

        isSearching = true;
        searchFound = null;
        searchMessage = string.Empty;
        foundCustomerId = null;

        try
        {
            var (customer, errorMessage) = await ApiClient.GetCustomerByPhoneAsync(command.PhoneNumber);

            if (customer != null)
            {
                foundCustomerId = customer.Id;
                searchFound = true;
                searchMessage = "مشتری یافت شد — اطلاعات بارگذاری شد";

                // Populate form with existing data
                command.Name = customer.Name;
                command.Gender = customer.Gender;
                command.BirthYear = customer.BirthYear;
                command.IsTester = customer.IsTester;
                command.BirthDate = customer.BirthDate.HasValue
                    ? PersianDateHelper.ToPersianDate(customer.BirthDate.Value)
                    : null;
            }
            else
            {
                searchFound = false;
                searchMessage = errorMessage ?? "مشتری با این شماره یافت نشد";
            }
        }
        catch
        {
            searchFound = false;
            searchMessage = "خطا در اتصال به سرور";
        }
        finally
        {
            isSearching = false;
        }
    }

    private async Task HandleSubmit()
    {
        isLoading = true;
        try
        {
            if (IsUpdateMode)
            {
                var updateCmd = new UpdateCustomerCommand
                {
                    Id = foundCustomerId!.Value,
                    Name = command.Name,
                    Gender = command.Gender,
                    BirthYear = command.BirthYear,
                    BirthDate = command.BirthDate,
                    IsTester = command.IsTester
                };
                var (success, message) = await ApiClient.UpdateCustomerAsync(updateCmd);
                if (success)
                    NotificationService.ShowSuccess(message);
                else
                    NotificationService.ShowError(message);
            }
            else
            {
                var (success, message) = await ApiClient.InsertCustomerAsync(command);
                if (success)
                {
                    NotificationService.ShowSuccess(message);
                    Reset();
                }
                else
                {
                    NotificationService.ShowError(message);
                }
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

    private void Reset()
    {
        command = new InsertCustomerCommand();
        foundCustomerId = null;
        searchFound = null;
        searchMessage = string.Empty;
    }
}
