using Microsoft.AspNetCore.Components;
using PineSms.UI.Services;

namespace PineSms.UI.Components.Pages;

public partial class Home
{
    [Inject] private AuthStateService AuthState { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized()
    {
        if (!AuthState.IsAuthenticated)
            Navigation.NavigateTo("/login");
    }
}
