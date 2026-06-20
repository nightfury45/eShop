using eShop.Admin.API.Apis;
using eShop.Admin.API.Infrastructure;
using eShop.Admin.API.Infrastructure.Analytics;
using eShop.Admin.API.Services;
using Microsoft.EntityFrameworkCore;

namespace eShop.Admin.UnitTests;

[TestClass]
public class AnalyticsQueriesTests
{
    private static readonly DateOnly Today = new(2026, 6, 30);

    private static AnalyticsDbContext SeededContext()
    {
        var ctx = new AnalyticsDbContext(
            new DbContextOptionsBuilder<AnalyticsDbContext>()
                .UseInMemoryDatabase($"queries-{Guid.NewGuid()}")
                .Options);

        // Current 7d window: 2026-06-24..30. Previous: 2026-06-17..23.
        ctx.DailySales.Add(new DailySalesFact { Date = new(2026, 6, 30), Revenue = 100m, Orders = 4, Units = 10 });
        ctx.DailySales.Add(new DailySalesFact { Date = new(2026, 6, 28), Revenue = 50m, Orders = 2, Units = 5 });
        ctx.DailySales.Add(new DailySalesFact { Date = new(2026, 6, 20), Revenue = 75m, Orders = 3, Units = 6 }); // previous window
        ctx.CategoryDaily.Add(new CategoryDailyFact { Date = new(2026, 6, 30), Category = "Footwear", Revenue = 90m, Units = 7 });
        ctx.CategoryDaily.Add(new CategoryDailyFact { Date = new(2026, 6, 28), Category = "Apparel", Revenue = 60m, Units = 8 });
        ctx.ProductDaily.Add(new ProductDailyFact { Date = new(2026, 6, 30), ProductId = 1, ProductName = "Aero", Category = "Footwear", Revenue = 90m, Units = 7 });
        ctx.ProductDaily.Add(new ProductDailyFact { Date = new(2026, 6, 28), ProductId = 2, ProductName = "Crew", Category = "Apparel", Revenue = 60m, Units = 8 });
        ctx.SaveChanges();
        return ctx;
    }

    [TestMethod]
    public async Task Summary_sums_current_window_and_computes_period_over_period_delta()
    {
        await using var ctx = SeededContext();
        var queries = new AnalyticsQueries(ctx);

        var summary = await queries.GetSummaryAsync(7, Today, default);

        Assert.AreEqual(150m, summary.Revenue.Value); // 100 + 50 (current window only)
        Assert.AreEqual(6, summary.Orders.Value);
        Assert.AreEqual(7, summary.RevenueSeries.Count);
        // Current revenue 150 vs previous 75 => +100%.
        Assert.AreEqual(100m, summary.Revenue.DeltaPercent);
    }

    [TestMethod]
    public async Task Summary_orders_top_categories_and_products_by_revenue_with_share()
    {
        await using var ctx = SeededContext();
        var queries = new AnalyticsQueries(ctx);

        var summary = await queries.GetSummaryAsync(7, Today, default);

        Assert.AreEqual("Footwear", summary.TopCategories[0].Category);
        Assert.AreEqual(1, summary.TopProducts[0].ProductId);
        Assert.AreEqual(60m, summary.TopProducts[0].SharePercent); // 90 of 150 total
    }

    [TestMethod]
    public void ParsePeriodDays_maps_known_periods_and_defaults_to_30()
    {
        Assert.AreEqual(7, AnalyticsApi.ParsePeriodDays("7d"));
        Assert.AreEqual(30, AnalyticsApi.ParsePeriodDays("30d"));
        Assert.AreEqual(90, AnalyticsApi.ParsePeriodDays("90d"));
        Assert.AreEqual(30, AnalyticsApi.ParsePeriodDays(null));
        Assert.AreEqual(30, AnalyticsApi.ParsePeriodDays("bogus"));
    }
}
