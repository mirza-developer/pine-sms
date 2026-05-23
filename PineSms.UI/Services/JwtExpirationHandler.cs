using Microsoft.AspNetCore.Components;

namespace PineSms.UI.Services;

/// <summary>
/// Delegating handler that intercepts every outgoing API request.
/// - Before sending: if the stored JWT is already expired the user is logged out immediately.
/// - After receiving: a 401 Unauthorized response from the server also triggers logout.
/// Lazy wrappers are used to defer service resolution and avoid a circular-dependency
/// between ApiClientService ↔ AuthStateService.
/// </summary>
public class JwtExpirationHandler : DelegatingHandler
{
    private readonly Lazy<AuthStateService> authState;
    private readonly Lazy<NavigationManager> navigation;

    public JwtExpirationHandler(Lazy<AuthStateService> authState, Lazy<NavigationManager> navigation)
    {
        this.authState = authState;
        this.navigation = navigation;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Proactive check: logout before the request reaches the server
        if (authState.Value.IsAuthenticated && authState.Value.IsTokenExpired)
        {
            await authState.Value.LogoutAsync();
            navigation.Value.NavigateTo("/login");
            return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Reactive check: server rejected the token
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            && authState.Value.IsAuthenticated)
        {
            await authState.Value.LogoutAsync();
            navigation.Value.NavigateTo("/login");
        }

        return response;
    }
}
