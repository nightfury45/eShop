using eShop.Admin.API.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace eShop.Admin.API.Apis;

public static class AnalyticsApi
{
    public static IEndpointRouteBuilder MapAnalyticsApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/analytics").RequireAuthorization(AdminApi.AdminPolicy);
        group.MapGet("/summary", GetSummary);
        return app;
    }

    public static async Task<Ok<AnalyticsSummary>> GetSummary(
        IAnalyticsQueries queries,
        string? period,
        CancellationToken cancellationToken)
    {
        var days = ParsePeriodDays(period);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var summary = await queries.GetSummaryAsync(days, today, cancellationToken);
        return TypedResults.Ok(summary);
    }

    public static int ParsePeriodDays(string? period) => period switch
    {
        "7d" => 7,
        "90d" => 90,
        _ => 30,
    };
}
