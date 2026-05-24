using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using PineSms.Core.Features.MenuLink;
using PineSms.UI.Services;

namespace PineSms.UI.Components.Layout;

public partial class NavMenu : IDisposable
{
    [Inject] private AuthStateService AuthState { get; set; } = default!;
    [Inject] private MenuAccessStateService MenuAccess { get; set; } = default!;

    private List<MenuLinkDto> menuLinks = [];
    private IEnumerable<string> sections => menuLinks.Select(l => l.SectionLabel).Distinct();

    protected override void OnInitialized()
    {
        AuthState.AuthenticationStateChanged += OnAuthStateChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await MenuAccess.EnsureLoadedAsync();
            menuLinks = MenuAccess.GetLinks();
            StateHasChanged();
        }
    }

    private void OnAuthStateChanged(Task<AuthenticationState> _)
    {
        InvokeAsync(async () =>
        {
            await MenuAccess.RefreshAsync();
            menuLinks = MenuAccess.GetLinks();
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        AuthState.AuthenticationStateChanged -= OnAuthStateChanged;
    }
}
