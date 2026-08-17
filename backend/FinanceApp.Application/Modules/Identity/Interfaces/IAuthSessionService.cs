using Microsoft.AspNetCore.Http;

namespace FinanceApp.Application.Modules.Identity.Interfaces;

public interface IAuthSessionService
{
    Task SignInAsync(
        HttpContext httpContext,
        long userId,
        string username,
        string email,
        string role,
        string securityStamp);

    Task SignOutAsync(HttpContext httpContext);
}
