using PineSms.Core.Features.MenuLink;

namespace PineSms.UI.Services;

/// <summary>
/// Scoped service (one per Blazor circuit) that caches the current user's
/// accessible menu links and exposes access-checking logic.
/// </summary>
public class MenuAccessStateService
{
    private readonly ApiClientService apiClientService;
    private readonly AuthStateService authStateService;

    private List<MenuLinkDto>? cachedLinks;
    private bool initialized;

    public MenuAccessStateService(ApiClientService apiClientService, AuthStateService authStateService)
    {
        this.apiClientService = apiClientService;
        this.authStateService = authStateService;
    }

    /// <summary>
    /// Loads accessible links from the API. Safe to call multiple times; caches after first load.
    /// Call after auth state is confirmed to be initialized.
    /// </summary>
    public async Task EnsureLoadedAsync()
    {
        if (initialized) return;
        await RefreshAsync();
    }

    /// <summary>Force-reloads accessible links from the API (call on login/logout).</summary>
    public async Task RefreshAsync()
    {
        initialized = false;
        cachedLinks = null;

        if (!authStateService.IsAuthenticated)
        {
            initialized = true;
            return;
        }

        try
        {
            cachedLinks = await apiClientService.GetMyMenuLinksAsync();
            // Ensure we never have a null list
            if (cachedLinks == null)
                cachedLinks = [];
        }
        catch (Exception ex)
        {
            // Log the error for diagnostics but don't fail - just use empty list
            Console.Error.WriteLine($"[MenuAccessStateService] Failed to load menu links: {ex.Message}");
            cachedLinks = [];
        }

        initialized = true;
    }

    /// <summary>Returns the cached list of accessible links for the current user.</summary>
    public List<MenuLinkDto> GetLinks() => cachedLinks ?? [];

    /// <summary>
    /// Returns true when the current user may navigate to the given URL path.
    /// Admins always have access; the home page is always accessible.
    /// </summary>
    public bool CanAccess(string urlPath)
    {
        if (!authStateService.IsAuthenticated) return false;
        if (authStateService.IsAdmin) return true;
        if (urlPath == "/" || urlPath == string.Empty) return true;

        return cachedLinks?.Any(l => l.Url.Equals(urlPath, StringComparison.OrdinalIgnoreCase)) ?? false;
    }

    /// <summary>Clears cached state (call on logout).</summary>
    public void Clear()
    {
        cachedLinks = null;
        initialized = false;
    }
}
