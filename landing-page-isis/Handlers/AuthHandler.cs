using System.Security.Claims;
using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Handlers;

public class AuthHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IAuthHandler
{
    private HttpContext? Context => httpContextAccessor.HttpContext;

    public async Task<HandlerResult> Login(string username, string password)
    {
        if (Context == null)
            return new HandlerResult(false, "Erro de conexão.");

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return new HandlerResult(false, "Credenciais inválidas.");
        }

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
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
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
