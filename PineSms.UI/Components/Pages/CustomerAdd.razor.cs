using Microsoft.AspNetCore.Components;
using PineSms.Core.Features.Customer;
using PineSms.Shared;
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
            var result = await ApiClient.GetCustomerByPhoneAsync(command.PhoneNumber);

            if (result.Customer != null)
            {
                foundCustomerId = result.Customer.Id;
                searchFound = true;
                searchMessage = "مشتری یافت شد — اطلاعات بارگذاری شد";

                // Populate form with existing data
                command.Name = result.Customer.Name;
                command.Gender = result.Customer.Gender;
                command.BirthYear = result.Customer.BirthYear;
                command.IsTester = result.Customer.IsTester;
                command.BirthDate = result.Customer.BirthDate.HasValue
                    ? PersianCalendarTools.GregorianToPersian(result.Customer.BirthDate.Value)
                    : null;
            }
            else
            {
                searchFound = false;
                searchMessage = result.ErrorMessage ?? "مشتری با این شماره یافت نشد";
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
                var result = await ApiClient.UpdateCustomerAsync(updateCmd);
                if (result.Success)
                    NotificationService.ShowSuccess(result.Message);
                else
                    NotificationService.ShowError(result.Message);
            }
            else
            {
                var result = await ApiClient.InsertCustomerAsync(command);
                if (result.Success)
                {
                    NotificationService.ShowSuccess(result.Message);
                    Reset();
                }
                else
                {
                    NotificationService.ShowError(result.Message);
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
