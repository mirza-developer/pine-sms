using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PineSms.Core.Features.Account;
using PineSms.UI.Services;

namespace PineSms.UI.Components.Pages;

[AllowAnonymous]
public partial class Login
{
    [Inject] private ApiClientService ApiClient { get; set; } = default!;
    [Inject] private AuthStateService AuthState { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private GetUserLoginQuery loginModel = new();
    private string errorMessage = string.Empty;
    private bool isLoading = false;

    protected override void OnInitialized()
    {
        if (AuthState.IsAuthenticated)
            Navigation.NavigateTo("/");
    }

    private async Task HandleLogin()
    {
        isLoading = true;
        errorMessage = string.Empty;
        try
        {
            var result = await ApiClient.LoginAsync(loginModel);
            if (result?.Success == true)
            {
                await AuthState.SetTokenAsync(result.Token);
                Navigation.NavigateTo("/");
            }
            else
            {
                errorMessage = result?.Message ?? "خطا در ورود";
            }
        }
        catch
        {
            errorMessage = "خطا در اتصال به سرور";
        }
        finally
        {
            isLoading = false;
        }
    }
}
