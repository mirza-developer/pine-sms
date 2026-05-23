using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace PineSms.UI.Services;

public class AuthStateService : AuthenticationStateProvider
{
    private readonly ApiClientService apiClientService;
    private readonly ProtectedLocalStorage localStorage;
    private const string TokenKey = "pine_sms_auth_token";
    private ClaimsPrincipal currentUser = new(new ClaimsIdentity());

    public AuthStateService(ApiClientService apiClientService, ProtectedLocalStorage localStorage)
    {
        this.apiClientService = apiClientService;
        this.localStorage = localStorage;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(currentUser));

    /// <summary>
    /// Restores auth state from localStorage. Must be called from OnAfterRenderAsync
    /// (JS interop is not available during static/server-side render).
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var result = await localStorage.GetAsync<string>(TokenKey);
            if (result.Success && !string.IsNullOrEmpty(result.Value))
            {
                ApplyToken(result.Value);
                if (IsTokenExpired)
                    await LogoutAsync();
            }
        }
        catch
        {
            // JS interop not ready (pre-render pass) – silently skip
        }
    }

    /// <summary>Saves token to localStorage and updates in-memory auth state.</summary>
    public async Task SetTokenAsync(string token)
    {
        try { await localStorage.SetAsync(TokenKey, token); } catch { }
        ApplyToken(token);
    }

    /// <summary>Clears token from localStorage and resets in-memory auth state.</summary>
    public async Task LogoutAsync()
    {
        try { await localStorage.DeleteAsync(TokenKey); } catch { }
        ClearState();
    }

    public bool IsAuthenticated => currentUser.Identity?.IsAuthenticated ?? false;
    public string UserName => currentUser.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
    public string PersianName => currentUser.FindFirst(ClaimTypes.Surname)?.Value ?? string.Empty;
    public string UserId => currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    public bool IsTokenExpired
    {
        get
        {
            var expClaim = currentUser.FindFirst("exp");
            if (expClaim == null) return false;
            return long.TryParse(expClaim.Value, out var exp)
                && DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= exp;
        }
    }

    private void ApplyToken(string token)
    {
        apiClientService.SetToken(token);
        currentUser = new ClaimsPrincipal(ParseToken(token));
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private void ClearState()
    {
        apiClientService.ClearToken();
        currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static ClaimsIdentity ParseToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return new ClaimsIdentity(jwtToken.Claims, "jwt");
        }
        catch
        {
            return new ClaimsIdentity();
        }
    }
}
