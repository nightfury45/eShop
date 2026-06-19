using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;

namespace eShop.Admin.API.Apis;

public static class AdminApi
{
    public const string AdminPolicy = "Admin";

    public static IEndpointRouteBuilder MapAdminApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin");

        // Anonymous: liveness probe + the OIDC client config the SPA needs to start a login.
        group.MapGet("/ping", Ping);
        group.MapGet("/config", GetClientConfig);

        // Everything else requires an authenticated administrator.
        var secured = group.MapGroup(string.Empty).RequireAuthorization(AdminPolicy);
        secured.MapGet("/me", GetMe);

        return app;
    }

    public static Ok<PingResponse> Ping() =>
        TypedResults.Ok(new PingResponse("ok", DateTime.UtcNow));

    public static Ok<AdminClientConfig> GetClientConfig(IConfiguration configuration) =>
        TypedResults.Ok(new AdminClientConfig(
            Authority: configuration["Identity:Url"] ?? string.Empty,
            ClientId: "adminspa",
            Scope: "openid profile admin offline_access"));

    public static Ok<AdminUser> GetMe(ClaimsPrincipal user) =>
        TypedResults.Ok(new AdminUser(
            Subject: user.FindFirstValue("sub") ?? string.Empty,
            Name: user.FindFirstValue("name") ?? user.Identity?.Name ?? string.Empty,
            Roles: user.FindAll("role").Select(c => c.Value).ToArray()));
}

public record PingResponse(string Status, DateTime Utc);

public record AdminClientConfig(string Authority, string ClientId, string Scope);

public record AdminUser(string Subject, string Name, string[] Roles);
