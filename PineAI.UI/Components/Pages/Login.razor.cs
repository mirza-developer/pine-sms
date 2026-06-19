using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PineAI.Core.Features.Account;
using PineAI.UI.Services;

namespace PineAI.UI.Components.Pages;

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
#if DEBUG
        loginModel = new()
        {
            Password = "Admin@123",
            Username = "admin"
        };
#endif
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
