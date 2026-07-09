using System.Security.Claims;
using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace landing_page_isis.Handlers;

/// <summary>
/// Handles admin authentication flows including credentials verification, session cookie issuance, and logout.
/// </summary>
public class AuthHandler(AppDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    : IAuthHandler
{
    private HttpContext? Context => httpContextAccessor.HttpContext;

    public async Task<HandlerResult> Login(string username, string password)
    {
        if (Context == null)
            return new HandlerResult(false, "Erro de conexão.");

        // Query the database for the user email (acting as the username)
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == username);

        // Verify password hash using BCrypt to prevent timing attacks and plaintext comparisons
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return new HandlerResult(false, "Credenciais inválidas.");
        }

        // Establish the user session identity via claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new("UserId", user.Id.ToString()), // Store UserId in session for downstream auditing/logic
        };

        var claimsIdentity = new ClaimsIdentity(claims, "Cookies");

        // Keep the authentication cookie persistent for up to 8 hours
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
        };

        // Write the encrypted cookie back to the HTTP response
        await Context.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity), authProperties);

        return new HandlerResult(true);
    }

    public async Task<bool> Logout()
    {
        if (Context == null)
            return false;
        
        // Terminate cookie session and clear client authentication state
        await Context.SignOutAsync();
        return true;
    }
}
