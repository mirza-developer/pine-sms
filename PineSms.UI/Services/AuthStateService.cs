using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace PineSms.UI.Services;

public class AuthStateService : AuthenticationStateProvider
{
    private readonly ApiClientService apiClientService;
    private ClaimsPrincipal currentUser = new(new ClaimsIdentity());
    private string? storedToken;

    public AuthStateService(ApiClientService apiClientService)
    {
        this.apiClientService = apiClientService;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(currentUser));
    }

    public void SetToken(string token)
    {
        storedToken = token;
        apiClientService.SetToken(token);
        var identity = ParseToken(token);
        currentUser = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void Logout()
    {
        storedToken = null;
        apiClientService.ClearToken();
        currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public bool IsAuthenticated => currentUser.Identity?.IsAuthenticated ?? false;
    public string UserName => currentUser.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
    public string PersianName => currentUser.FindFirst(ClaimTypes.Surname)?.Value ?? string.Empty;
    public string UserId => currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

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
