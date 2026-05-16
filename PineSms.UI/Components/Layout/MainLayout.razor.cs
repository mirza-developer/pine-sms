using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using PineSms.UI.Services;

namespace PineSms.UI.Components.Layout;

public partial class MainLayout : IDisposable
{
    [Inject] private AuthStateService AuthState { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private bool collapseNavMenu = true;
    private bool isAuthInitialized = false;

    protected override void OnInitialized()
    {
        Navigation.LocationChanged += OnLocationChanged;
        AuthState.AuthenticationStateChanged += OnAuthStateChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try { await AuthState.InitializeAsync(); } catch { }
            isAuthInitialized = true;
            StateHasChanged();
        }
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        collapseNavMenu = true;
        InvokeAsync(StateHasChanged);
    }

    private void OnAuthStateChanged(Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> task)
    {
        InvokeAsync(StateHasChanged);
    }

    private void ToggleNavMenu() => collapseNavMenu = !collapseNavMenu;
    private async Task Logout()
    {
        await AuthState.LogoutAsync();
        Navigation.NavigateTo("/login");
    }

    public void Dispose()
    {
        Navigation.LocationChanged -= OnLocationChanged;
        AuthState.AuthenticationStateChanged -= OnAuthStateChanged;
    }
}
