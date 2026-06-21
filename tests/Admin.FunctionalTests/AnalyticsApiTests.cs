using System.Net;
using System.Net.Http.Json;
using eShop.Admin.API.Infrastructure;
using eShop.Admin.API.Infrastructure.Analytics;
using eShop.Admin.API.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace eShop.Admin.FunctionalTests;

public sealed class AnalyticsApiTests : IClassFixture<AdminApiFixture>
{
    private readonly AdminApiFixture _fixture;

    public AnalyticsApiTests(AdminApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SummaryReturnsUnauthorizedWithoutAdminPrincipal()
    {
        var httpClient = _fixture.CreateDefaultClient();

        var response = await httpClient.GetAsync("/api/admin/analytics/summary", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SummaryReflectsSeededFactsForAuthorizedAdmin()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
            db.DailySales.Add(new DailySalesFact { Date = today, Revenue = 123m, Orders = 2, Units = 5 });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var httpClient = _fixture.CreateDefaultClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/analytics/summary?period=7d");
        request.Headers.Add(AdminAutoAuthorizeMiddleware.AdminHeader, "true");

        var response = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<AnalyticsSummary>(TestContext.Current.CancellationToken);
        Assert.NotNull(summary);
        Assert.Equal(7, summary.PeriodDays);
        Assert.Equal(7, summary.RevenueSeries.Count);
        Assert.True(summary.Revenue.Value >= 123m);
        Assert.True(summary.Orders.Value >= 2m);
    }
}
