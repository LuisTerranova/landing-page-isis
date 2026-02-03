namespace landing_page_isis.core.Interfaces;

public interface IAuthHandler
{
    Task<bool> Login(string username, string password);
    Task<bool> Logout();
}
