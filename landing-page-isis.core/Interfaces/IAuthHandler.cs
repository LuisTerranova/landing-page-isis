namespace landing_page_isis.core.Interfaces;

public interface IAuthHandler
{
    Task<HandlerResult> Login(string username, string password);
    Task<bool> Logout();
}