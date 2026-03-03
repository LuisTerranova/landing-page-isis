using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace landing_page_isis.Authentication;

public class AuthProvider(IHttpContextAccessor _accessor) : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = _accessor.HttpContext?.User ?? new ClaimsPrincipal();
        return Task.FromResult(new AuthenticationState(user));
    }
}
