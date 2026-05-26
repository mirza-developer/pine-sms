using Microsoft.AspNetCore.Components;
using PineSms.Core.Features.MenuLink;
using PineSms.UI.Services;

namespace PineSms.UI.Components.Pages;
public partial class Home
{
    public List<MenuLinkDto> AccessibleLinks = [];

    [Inject] private AuthStateService AuthState { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        if (!AuthState.IsAuthenticated)
            Navigation.NavigateTo("/login");

        await MenuAccess.EnsureLoadedAsync();

        AccessibleLinks = MenuAccess.GetLinks().Where(l => l.Url != "/").ToList();

        StateHasChanged();
    }

    public static string GetCardColor(string iconName) => iconName switch
    {
        var n when n.Contains("person") => "success",
        var n when n.Contains("excel") => "info",
        var n when n.Contains("box") => "warning",
        var n when n.Contains("tags") => "primary",
        var n when n.Contains("key") => "secondary",
        var n when n.Contains("chat") => "dark",
        _ => "primary"
    };
}
