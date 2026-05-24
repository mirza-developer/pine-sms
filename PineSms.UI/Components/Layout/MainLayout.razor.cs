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
    [Inject] private MenuAccessStateService MenuAccess { get; set; } = default!;

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
            await MenuAccess.RefreshAsync();
            isAuthInitialized = true;
            CheckAccess(Navigation.Uri);
            StateHasChanged();
        }
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        collapseNavMenu = true;
        InvokeAsync(async () =>
        {
            await SetBodyScrollLock(false);
            CheckAccess(e.Location);
            StateHasChanged();
        });
    }

    private void OnAuthStateChanged(Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> task)
    {
        InvokeAsync(async () =>
        {
            await MenuAccess.RefreshAsync();
            CheckAccess(Navigation.Uri);
            StateHasChanged();
        });
    }

    /// <summary>
    /// Checks whether the current user may access the given absolute URL.
    /// If not, redirects to /access-denied.
    /// Unauthenticated users are handled by the route guard (redirect to /login).
    /// </summary>
    private void CheckAccess(string absoluteUri)
    {
        if (!isAuthInitialized) return;
        if (!AuthState.IsAuthenticated) return;

        var uri = new Uri(absoluteUri);
        var path = uri.AbsolutePath.TrimEnd('/');
        if (string.IsNullOrEmpty(path)) path = "/";

        // These routes are always accessible and bypass the menu-link check
        if (path is "/" or "/login" or "/access-denied") return;

        // Admin users have no restrictions
        if (AuthState.IsAdmin) return;

        if (!MenuAccess.CanAccess(path))
        {
            Navigation.NavigateTo("/access-denied", replace: true);
        }
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
        MenuAccess.Clear();
        Navigation.NavigateTo("/login");
    }

    public void Dispose()
    {
        Navigation.LocationChanged -= OnLocationChanged;
        AuthState.AuthenticationStateChanged -= OnAuthStateChanged;
        try { JS.InvokeVoidAsync("eval", "document.body.classList.remove('nav-open')"); } catch { }
    }
}
