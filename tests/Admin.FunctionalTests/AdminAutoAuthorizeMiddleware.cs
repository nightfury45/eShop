using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace eShop.Admin.FunctionalTests;

/// <summary>
/// Test-only middleware: when a request carries the <see cref="AdminHeader"/>, an authenticated
/// administrator principal is injected so policy-protected endpoints can be exercised without a real
/// OIDC token. Requests without the header stay anonymous (to assert 401 behaviour).
/// </summary>
public sealed class AdminAutoAuthorizeMiddleware(RequestDelegate next)
{
    public const string AdminHeader = "X-Test-Admin";

    public async Task Invoke(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.ContainsKey(AdminHeader))
        {
            var identity = new ClaimsIdentity(authenticationType: "TestAuth", nameType: "name", roleType: "role");
            identity.AddClaim(new Claim("sub", "admin-test"));
            identity.AddClaim(new Claim("name", "Test Admin"));
            identity.AddClaim(new Claim("role", "Administrator"));
            httpContext.User.AddIdentity(identity);
        }

        await next(httpContext);
    }
}
