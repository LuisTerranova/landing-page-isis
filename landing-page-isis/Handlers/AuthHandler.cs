using landing_page_isis.core;

namespace landing_page_isis;

public class AuthHandler : IAuthHandler
{
    public Task<bool> Login(string username, string password)
    {
        throw new NotImplementedException();
    }

    public Task<bool> Logout()
    {
        throw new NotImplementedException();
    }
}
