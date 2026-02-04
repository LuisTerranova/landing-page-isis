using System.Security.Claims;
using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace landing_page_isis;

public class AuthHandler(AppDbContext dbContext, HttpContext context) : IAuthHandler
{
    public async Task<bool> Login(string username, string password)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == username);

        if (user == null)
            return false;

        var senhaValida = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

        if (!senhaValida)
            return false;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("UsuarioId", user.Id.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, "Cookies");

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(3)
        };

        await context.SignInAsync(
            "Cookies",
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return true;
    }

    public async Task<bool> Logout()
    {
        await context.SignOutAsync();
        return true;
    }
}