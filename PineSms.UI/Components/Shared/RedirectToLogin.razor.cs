using Microsoft.AspNetCore.Components;

namespace PineSms.UI.Components.Shared;

public partial class RedirectToLogin
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized()
    {
        Navigation.NavigateTo("/login");
    }
}
