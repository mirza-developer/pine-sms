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
        catch (Exception ex)
        {
            // JS interop not ready (pre-render pass) or other storage error – silently skip
            Console.Error.WriteLine($"[AuthStateService] InitializeAsync failed (expected during pre-render): {ex.Message}");
        }
    }

    /// <summary>Saves token to localStorage and updates in-memory auth state.</summary>
    public async Task SetTokenAsync(string token)
    {
        try 
        { 
            await localStorage.SetAsync(TokenKey, token); 
        } 
        catch (Exception ex) 
        {
            // Log but don't fail - token is still set in memory for this session
            Console.Error.WriteLine($"[AuthStateService] Failed to persist token to localStorage: {ex.Message}");
        }

        ApplyToken(token);
    }

    /// <summary>Clears token from localStorage and resets in-memory auth state.</summary>
    public async Task LogoutAsync()
    {
        try 
        { 
            await localStorage.DeleteAsync(TokenKey); 
        } 
        catch (Exception ex) 
        {
            // Log but continue with logout - in-memory state will still be cleared
            Console.Error.WriteLine($"[AuthStateService] Failed to delete token from localStorage: {ex.Message}");
        }
        ClearState();
    }

    public bool IsAuthenticated => currentUser.Identity?.IsAuthenticated ?? false;
    public string UserName => currentUser.FindFirst("unique_name")?.Value
                           ?? currentUser.FindFirst(ClaimTypes.Name)?.Value
                           ?? string.Empty;
    public string PersianName => currentUser.FindFirst("family_name")?.Value
                              ?? currentUser.FindFirst(ClaimTypes.Surname)?.Value
                              ?? string.Empty;
    public string UserId => currentUser.FindFirst("nameid")?.Value
                         ?? currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? string.Empty;
    public bool IsAdmin => currentUser.FindFirst("role")?.Value == "Admin"
                        || currentUser.IsInRole("Admin");

    public bool IsTokenExpired
    {
        get
        {
            var expClaim = currentUser.FindFirst("exp");
            // No expiration claim = treat as expired for safety
            if (expClaim == null) return true;

            // Invalid expiration value = treat as expired
            if (!long.TryParse(expClaim.Value, out var exp)) return true;

            // Add 60-second grace period for clock skew between client and server
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= (exp - 60);
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
