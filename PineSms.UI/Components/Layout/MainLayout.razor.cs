using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using PineSms.UI.Services;

namespace PineSms.UI.Components.Layout;

public partial class MainLayout : IDisposable
{
    [Inject] private AuthStateService AuthState { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

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
        InvokeAsync(async () =>
        {
            await SetBodyScrollLock(false);
            StateHasChanged();
        });
    }

    private void OnAuthStateChanged(Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> task)
    {
        InvokeAsync(StateHasChanged);
    }

    private async Task ToggleNavMenu()
    {
        collapseNavMenu = !collapseNavMenu;
        await SetBodyScrollLock(!collapseNavMenu);
    }

    private async Task SetBodyScrollLock(bool locked)
    {
        try
        {
            await JS.InvokeVoidAsync("eval",
                locked ? "document.body.classList.add('nav-open')" : "document.body.classList.remove('nav-open')");
        }
        catch { }
    }
    private async Task Logout()
    {
        await AuthState.LogoutAsync();
        Navigation.NavigateTo("/login");
    }

    public void Dispose()
    {
        Navigation.LocationChanged -= OnLocationChanged;
        AuthState.AuthenticationStateChanged -= OnAuthStateChanged;
        try { JS.InvokeVoidAsync("eval", "document.body.classList.remove('nav-open')"); } catch { }
    }
}
