using Microsoft.AspNetCore.Builder;

namespace eShop.Admin.FunctionalTests;

/// <summary>Inserts <see cref="AdminAutoAuthorizeMiddleware"/> at the start of the pipeline.</summary>
public sealed class AdminAutoAuthorizeStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        builder =>
        {
            builder.UseMiddleware<AdminAutoAuthorizeMiddleware>();
            next(builder);
        };
}
