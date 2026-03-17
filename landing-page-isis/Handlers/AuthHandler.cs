using System.Security.Claims;
using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace landing_page_isis.Handlers;

public class AuthHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    IMemoryCache cache
) : IAuthHandler
{
    private HttpContext? Context => httpContextAccessor.HttpContext;

    public async Task<HandlerResult> Login(string username, string password)
    {
        if (Context == null)
            return new HandlerResult(false, "Erro de conexão.");

        var ip = Context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var cacheKey = $"login_attempts_{ip}";

        if (cache.TryGetValue(cacheKey, out int attempts) && attempts >= 5)
            return new HandlerResult(false, "Muitas tentativas. Aguarde 5 minutos.");

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            cache.Set(cacheKey, attempts + 1, TimeSpan.FromMinutes(5));
            return new HandlerResult(false, "Credenciais inválidas.");
        }

        cache.Remove(cacheKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new("UserId", user.Id.ToString()),
        };

        var claimsIdentity = new ClaimsIdentity(claims, "Cookies");

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(3),
        };

        await Context.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity), authProperties);

        return new HandlerResult(true);
    }

    public async Task<bool> Logout()
    {
        if (Context == null)
            return false;
        await Context.SignOutAsync();
        return true;
    }
}
