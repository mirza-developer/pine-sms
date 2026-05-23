using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using PineSms.UI.Services;

namespace PineSms.UI.Components.Layout;

public partial class NavMenu : IDisposable
{
    [Inject] private AuthStateService AuthState { get; set; } = default!;

    protected override void OnInitialized()
    {
        AuthState.AuthenticationStateChanged += OnAuthStateChanged;
    }

    private void OnAuthStateChanged(Task<AuthenticationState> _)
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        AuthState.AuthenticationStateChanged -= OnAuthStateChanged;
    }
}
