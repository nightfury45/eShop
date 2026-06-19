using Microsoft.AspNetCore.Http.HttpResults;

namespace eShop.Admin.API.Apis;

public static class AdminApi
{
    public static IEndpointRouteBuilder MapAdminApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin");

        // Liveness/round-trip probe used by the SPA to verify BFF connectivity in Epic 0.
        group.MapGet("/ping", Ping);

        return app;
    }

    public static Ok<PingResponse> Ping() =>
        TypedResults.Ok(new PingResponse("ok", DateTime.UtcNow));
}

public record PingResponse(string Status, DateTime Utc);
