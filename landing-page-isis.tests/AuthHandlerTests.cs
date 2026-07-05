using System.Security.Claims;
using landing_page_isis.core;
using landing_page_isis.core.Interfaces;
using landing_page_isis.Handlers;
using landing_page_isis.Infrastructure.Data;
using landing_page_isis.core.ApplicationUser;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace landing_page_isis.tests;

public class AuthHandlerTests
{
    private AppDbContext GetDatabaseContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection, contextOwnsConnection: true)
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        Environment.SetEnvironmentVariable("ENCRYPTION_KEY", "test-encryption-key-32-bytes----");

        return context;
    }

    private (AuthHandler Handler, Mock<IAuthenticationService> AuthMock) CreateHandler(AppDbContext context)
    {
        var authMock = new Mock<IAuthenticationService>();

        var serviceProvider = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(authMock.Object)
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        return (new AuthHandler(context, httpContextAccessorMock.Object), authMock);
    }

    [Fact]
    public async Task Login_ShouldReturnTrue_WhenCredentialsValid()
    {
        await using var context = GetDatabaseContext();
        var (handler, authMock) = CreateHandler(context);

        var password = "senha123";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        context.Users.Add(new User
        {
            Name = "Admin",
            Email = "admin@isis.com",
            PasswordHash = passwordHash,
        });
        await context.SaveChangesAsync();

        authMock
            .Setup(a => a.SignInAsync(
                It.IsAny<HttpContext>(),
                "Cookies",
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        var result = await handler.Login("admin@isis.com", password);

        Assert.True(result.Success);
        authMock.Verify(
            a => a.SignInAsync(
                It.IsAny<HttpContext>(),
                "Cookies",
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Once);
    }

    [Fact]
    public async Task Login_ShouldReturnFalse_WhenUserNotFound()
    {
        await using var context = GetDatabaseContext();
        var (handler, _) = CreateHandler(context);

        var result = await handler.Login("notfound@email.com", "qualquer");

        Assert.False(result.Success);
        Assert.Equal("Credenciais inválidas.", result.Message);
    }

    [Fact]
    public async Task Login_ShouldReturnFalse_WhenPasswordWrong()
    {
        await using var context = GetDatabaseContext();
        var (handler, _) = CreateHandler(context);

        context.Users.Add(new User
        {
            Name = "Admin",
            Email = "admin@isis.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("senha_correta"),
        });
        await context.SaveChangesAsync();

        var result = await handler.Login("admin@isis.com", "senha_errada");

        Assert.False(result.Success);
        Assert.Equal("Credenciais inválidas.", result.Message);
    }

    [Fact]
    public async Task Logout_ShouldReturnTrue()
    {
        await using var context = GetDatabaseContext();
        var (handler, _) = CreateHandler(context);

        var result = await handler.Logout();

        Assert.True(result);
    }
}
