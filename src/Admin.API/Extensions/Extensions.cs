using Microsoft.AspNetCore.Authentication.JwtBearer;

public static class Extensions
{
    /// <summary>
    /// Registers Admin Dashboard BFF application services: the dashboard-owned OLTP database (admindb),
    /// inbound JWT authentication against the eShop Identity provider, and the admin authorization policy.
    /// Later epics add cross-service HttpClients, the event bus, and the analytics (OLAP) context.
    /// </summary>
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<AdminDbContext>("admindb");

        // Validate JWTs issued by Identity.API (reads Identity:Url + Identity:Audience).
        builder.AddDefaultAuthentication();

        // Identity.API's ProfileService emits roles as "role" claims; map them for RequireRole.
        builder.Services.Configure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options => options.TokenValidationParameters.RoleClaimType = "role");

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("Admin", policy => policy.RequireRole("Administrator"));
    }
}
